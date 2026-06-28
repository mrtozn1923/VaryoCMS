using VaryoCms.Application.Common;

namespace VaryoCms.Application.Interfaces;

public interface ILoginCodeService
{
    // appsettings'den — SystemAdmin girişi için.
    bool IsSystemEnabled { get; }

    // DB'den — Tenant girişi için per-tenant kontrol.
    Task<bool> IsTenantEnabledAsync(int tenantId, CancellationToken ct = default);

    // Generates a code, persists it, and emails it. Uses appsettings for system, DB for tenant.
    Task SendCodeAsync(string email, string tenantType, int? tenantId, CancellationToken ct = default);

    // Verifies the submitted code. Error values:
    //   "ExpiredCode" | "MaxAttemptsExceeded" | "NotFound" → redirect to login
    //   "InvalidCode:{remaining}"                          → show on form
    Task<Result> VerifyAsync(string email, string tenantType, int? tenantId, string code, CancellationToken ct = default);
}
