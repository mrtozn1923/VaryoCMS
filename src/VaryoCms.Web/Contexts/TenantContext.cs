using VaryoCms.Application.Interfaces;

namespace VaryoCms.Web.Contexts;

// Scoped implementation. Middleware resolves the tenant and calls Set(...);
// everything else consumes the read-only ITenantContext.
public class TenantContext : ITenantContext
{
    public int TenantId { get; private set; }
    public string TenantSlug { get; private set; } = string.Empty;
    public string DefaultLanguageCode { get; private set; } = "tr";
    public bool IsResolved { get; private set; }

    public void Set(int tenantId, string tenantSlug, string defaultLanguageCode)
    {
        TenantId = tenantId;
        TenantSlug = tenantSlug;
        DefaultLanguageCode = defaultLanguageCode;
        IsResolved = true;
    }
}
