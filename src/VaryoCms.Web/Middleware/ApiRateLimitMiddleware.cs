using VaryoCms.Domain.Interfaces.Repositories;
using VaryoCms.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace VaryoCms.Web.Middleware;

// Fixed-window (per minute) in-memory rate limiter for the public API only.
// Limit per (tenant, content type) comes from api_configurations.rate_limit_per_min (cached 60s).
// Counter is keyed by tenant + content type + client IP.
public class ApiRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public ApiRateLimitMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, ITenantStore tenantStore, IPublicApiRepository repo)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api/v1") || path.Value is null)
        {
            await _next(context);
            return;
        }

        // /api/v1/{tenant}/{contentType}/...
        var segments = path.Value.Trim('/').Split('/');
        if (segments.Length < 4)
        {
            await _next(context);
            return;
        }
        string tenantSlug = segments[2];
        string ctSlug = segments[3];

        int limit = await _cache.GetOrCreateAsync($"rl-limit:{tenantSlug}:{ctSlug}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            var tenant = await tenantStore.FindBySlugAsync(tenantSlug);
            if (tenant is null) return 0;
            var config = await repo.GetEnabledConfigAsync(tenant.Id, ctSlug);
            return config?.RateLimitPerMin ?? 60;
        });

        // 0 => no enabled API config for this route; don't rate limit (controller will 404).
        if (limit <= 0)
        {
            await _next(context);
            return;
        }

        string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var counter = _cache.GetOrCreate($"rl:{tenantSlug}:{ctSlug}:{ip}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);   // window; not reset on increment
            return new RateCounter();
        })!;

        int count = Interlocked.Increment(ref counter.Count);

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - count).ToString();

        if (count > limit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            return;
        }

        await _next(context);
    }

    private sealed class RateCounter
    {
        public int Count;
    }
}
