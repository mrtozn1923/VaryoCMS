namespace VaryoCms.Application.Interfaces;

// Scoped, per-request. Resolved by TenantResolutionMiddleware, consumed by services/repositories.
public interface ITenantContext
{
    int TenantId { get; }
    string TenantSlug { get; }
    string DefaultLanguageCode { get; }
    bool IsResolved { get; }
}
