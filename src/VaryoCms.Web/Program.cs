using System.Threading.RateLimiting;
using VaryoCms.Application;
using VaryoCms.Application.Interfaces;
using VaryoCms.Infrastructure;   // composition root ONLY — AddInfrastructure(); no Infrastructure types used elsewhere in Web
using VaryoCms.Web.Contexts;
using VaryoCms.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Serilog: configuration-driven (SQL Server sink by default; Graylog added via appsettings WriteTo).
builder.Host.UseSerilog((ctx, sp, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(sp)
    .Enrich.FromLogContext());

// DB-backed admin-UI localization. The custom IStringLocalizerFactory (AddInfrastructure) feeds all three.
builder.Services.AddLocalization();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Secure by default: every endpoint requires an authenticated user unless marked [AllowAnonymous].
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
})
.AddViewLocalization()
.AddDataAnnotationsLocalization();

// Platform-console controllers (Controllers/System/*) keep their views grouped under /Views/System/{Controller}.
builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(o =>
{
    o.ViewLocationFormats.Add("/Views/System/{1}/{0}" + Microsoft.AspNetCore.Mvc.Razor.RazorViewEngine.ViewExtension);
});

// Allow the antiforgery token via header so AJAX (e.g. field reorder PATCH) can be validated.
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

// In-memory cache backs the public API rate limiter.
builder.Services.AddMemoryCache();

// Fixed-window rate limiter for login endpoints: 10 attempts per 5 minutes, PARTITIONED PER CLIENT IP.
// A single shared limiter would let one attacker's attempts lock out every legitimate user (DoS) and
// wouldn't isolate brute-force per source — so we key the partition on the remote IP.
builder.Services.AddRateLimiter(opts =>
{
    opts.AddPolicy("login", httpContext =>
    {
        string partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });
    });
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Cookie authentication for the admin area (JWT is reserved for the public API).
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Dev runs on plain HTTP (localhost:5267) — Always would drop the cookie there.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

// Application services + validators, then data access. Registered at the composition root.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Current-user context reads claims from the request's principal.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, VaryoCms.Web.Contexts.CurrentUserContext>();

// Scoped per-request contexts: register concrete (for middleware to Set) + expose read-only interfaces.
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<LanguageContext>();
builder.Services.AddScoped<ILanguageContext>(sp => sp.GetRequiredService<LanguageContext>());
// ITenantStore is provided by AddInfrastructure (Dapper TenantStore).

// Web-side view-model composition helpers.
builder.Services.AddScoped<VaryoCms.Web.Support.ContentItemFormFactory>();

// File storage needs the physical web root, so it is wired at the composition root.
builder.Services.AddSingleton<IFileStorageService>(
    new VaryoCms.Infrastructure.Storage.LocalFileStorageService(
        builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"),
        "/uploads"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Security headers — applied to every response before routing.
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    // TinyMCE (RichText editor) and Monaco (CodeSnippet/JSON) require unsafe-inline.
    // cdn.jsdelivr.net serves TinyMCE; cdnjs.cloudflare.com serves Monaco.
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' cdn.jsdelivr.net cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' cdn.jsdelivr.net rsms.me fonts.googleapis.com; " +
        "font-src 'self' cdn.jsdelivr.net rsms.me fonts.gstatic.com; " +
        "img-src 'self' data: blob:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'");
    await next();
});

if (!app.Environment.IsEnvironment("Testing")) app.UseHttpsRedirection();

// Static assets. User-uploaded media lives under wwwroot/uploads and is served by this same
// middleware. Defense-in-depth: even though the storage layer only ever writes a server-controlled
// extension from a raster/doc allow-map (no SVG/HTML/JS), lock down responses for /uploads so a
// stray active-content file can never execute in the app origin — a sandbox CSP neutralises inline
// scripts and nosniff blocks MIME sniffing.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Path.StartsWithSegments("/uploads"))
        {
            ctx.Context.Response.Headers["Content-Security-Policy"] = "sandbox; default-src 'none'";
            ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        }
    }
});

app.UseRouting();

// Rate limiter must come after UseRouting so [EnableRateLimiting] attributes are resolved per-endpoint.
app.UseRateLimiter();

// Tenant first, then language (language default comes from the resolved tenant).
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<LanguageResolutionMiddleware>();

app.UseAuthentication();

// After authentication: a SystemAdmin impersonating a tenant overrides the resolved tenant/language context.
app.UseMiddleware<ImpersonationMiddleware>();

// Bind the authenticated (non-SystemAdmin) session to the resolved tenant: reject if the cookie's
// tenant claim doesn't match the request's tenant. Must run after impersonation (which sets the
// context) and before authorization.
app.UseMiddleware<TenantAccessGuardMiddleware>();

app.UseAuthorization();

// Serilog request logging: only 4xx/5xx reach the SQL sink (200/3xx stay at Debug and are filtered).
app.UseSerilogRequestLogging(opts =>
{
    opts.GetLevel = (http, _, ex) =>
        ex is not null || http.Response.StatusCode >= 500 ? LogEventLevel.Error
        : http.Response.StatusCode >= 400                 ? LogEventLevel.Warning
        : LogEventLevel.Debug;

    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        ITenantContext? tenant = http.RequestServices.GetService<ITenantContext>();
        ICurrentUserContext? user = http.RequestServices.GetService<ICurrentUserContext>();
        if (tenant?.IsResolved == true)
        {
            diag.Set("TenantId",   tenant.TenantId);
            diag.Set("TenantSlug", tenant.TenantSlug);
        }
        if (user?.UserId is int uid)
        {
            diag.Set("UserId", uid);
            diag.Set("Role",   user.Role?.ToString());
        }
        diag.Set("RequestPath", http.Request.Path.Value);
    };
});

// Rate limiting applies only to /api/v1 routes (self-filtered inside the middleware).
app.UseMiddleware<VaryoCms.Web.Middleware.ApiRateLimitMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Expose Program to WebApplicationFactory<Program> in test projects.
public partial class Program { }
