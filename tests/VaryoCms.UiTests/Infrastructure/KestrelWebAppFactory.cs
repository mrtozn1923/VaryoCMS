using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using VaryoCms.Application;
using VaryoCms.Application.Interfaces;
using VaryoCms.Infrastructure;
using VaryoCms.Infrastructure.Storage;
using VaryoCms.Web.Contexts;
using VaryoCms.Web.Middleware;

namespace VaryoCms.UiTests.Infrastructure;

/// <summary>
/// Starts the Varyo CMS application on a real Kestrel socket (port 0 = OS-assigned) for Playwright E2E tests.
///
/// WebApplicationFactory&lt;Program&gt; only supports in-memory TestServer, which Playwright cannot reach
/// because it needs a real TCP socket. This class replicates Program.cs service registrations and
/// middleware pipeline but injects test overrides (connection string, JWT key, etc.) first.
/// </summary>
public sealed class KestrelWebAppFactory : IAsyncLifetime
{
    private readonly string _connectionString;
    private WebApplication? _app;

    public string ServerAddress { get; private set; } = string.Empty;

    public KestrelWebAppFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        // --- Test overrides (must come before AddInfrastructure so lazy singletons pick them up) ---
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:VaryoCms"] = _connectionString,
            ["Jwt:SigningKey"]             = "test-signing-key-at-least-32-bytes-long!!",
            ["Jwt:Issuer"]                 = "VaryoCms",
            ["DevTenantSlug"]              = "dev-tenant",
            ["EmailVerification:Enabled"]  = "false",
            // Suppress Serilog SQL sink in tests
            ["Serilog:WriteTo:0:Name"]     = "Console",
        });

        builder.Environment.EnvironmentName = "Testing";

        // Point ContentRoot at the Web project directory so Razor views (disk-mode) and the
        // dev configuration files are resolved correctly during testing.
        string webProjectDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "VaryoCms.Web"));
        if (Directory.Exists(webProjectDir))
            builder.Environment.ContentRootPath = webProjectDir;

        // UseUrls on the IWebHostBuilder extension — exposes the random port via IServerAddressesFeature
        ((IWebHostBuilder)builder.WebHost).UseUrls("http://127.0.0.1:0");

        // Suppress Serilog from appsettings so the SQL sink doesn't throw without a real DB
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Console()
            .CreateLogger();
        builder.Host.UseSerilog();

        // --- Service registrations (mirrors Program.cs) ---
        builder.Services.AddLocalization();

        // AddApplicationPart is critical: the entry assembly in tests is VaryoCms.UiTests.dll,
        // not VaryoCms.Web.dll. Without this, MVC won't discover controllers or compiled Razor views
        // from the web project, so all routes return 404.
        builder.Services.AddControllersWithViews(options =>
        {
            var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
        })
        .AddApplicationPart(typeof(Program).Assembly)   // discovers controllers + compiled views in VaryoCms.Web
        .AddViewLocalization()
        .AddDataAnnotationsLocalization();

        builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(o =>
        {
            o.ViewLocationFormats.Add("/Views/System/{1}/{0}" +
                Microsoft.AspNetCore.Mvc.Razor.RazorViewEngine.ViewExtension);
        });

        builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
        builder.Services.AddMemoryCache();

        builder.Services.AddRateLimiter(opts =>
        {
            // Use a very high limit in tests so the login rate limiter never fires.
            // Production uses 10/5min; tests make 1 login attempt per test class method.
            opts.AddFixedWindowLimiter("login", o =>
            {
                o.PermitLimit = 1000;
                o.Window = TimeSpan.FromMinutes(5);
                o.QueueLimit = 0;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
            opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath        = "/account/login";
                options.LogoutPath       = "/account/logout";
                options.AccessDeniedPath = "/account/access-denied";
                options.ExpireTimeSpan   = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly  = true;
                options.Cookie.SameSite  = SameSiteMode.Lax;
            });

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        builder.Services.AddScoped<TenantContext>();
        builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        builder.Services.AddScoped<LanguageContext>();
        builder.Services.AddScoped<ILanguageContext>(sp => sp.GetRequiredService<LanguageContext>());

        builder.Services.AddScoped<VaryoCms.Web.Support.ContentItemFormFactory>();

        string webRoot = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "VaryoCms.Web", "wwwroot");
        webRoot = Path.GetFullPath(webRoot);

        builder.Services.AddSingleton<IFileStorageService>(
            new LocalFileStorageService(webRoot, "/uploads"));

        // --- Build app ---
        _app = builder.Build();

        // --- Middleware pipeline (mirrors Program.cs) ---
        _app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"]        = "SAMEORIGIN";
            context.Response.Headers["X-XSS-Protection"]      = "1; mode=block";
            context.Response.Headers["Referrer-Policy"]        = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"]     = "camera=(), microphone=(), geolocation=()";
            await next();
        });

        // No HTTPS redirect in tests.
        _app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot)
        });

        _app.UseRouting();
        _app.UseRateLimiter();

        _app.UseMiddleware<TenantResolutionMiddleware>();
        _app.UseMiddleware<LanguageResolutionMiddleware>();
        _app.UseAuthentication();
        _app.UseMiddleware<ImpersonationMiddleware>();
        _app.UseAuthorization();

        _app.UseSerilogRequestLogging(opts =>
        {
            opts.GetLevel = (http, _, ex) =>
                ex is not null || http.Response.StatusCode >= 500 ? LogEventLevel.Error
                : http.Response.StatusCode >= 400                  ? LogEventLevel.Warning
                : LogEventLevel.Debug;
        });

        _app.UseMiddleware<ApiRateLimitMiddleware>();

        _app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        // Start on the random port
        await _app.StartAsync();

        // Discover actual bound address
        IServer server = _app.Services.GetRequiredService<IServer>();
        IServerAddressesFeature? addresses = server.Features.Get<IServerAddressesFeature>();
        ServerAddress = addresses?.Addresses.FirstOrDefault()?.TrimEnd('/')
            ?? throw new InvalidOperationException("Kestrel did not bind to an address.");
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        Log.CloseAndFlush();
    }
}
