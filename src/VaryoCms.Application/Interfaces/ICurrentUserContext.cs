using VaryoCms.Domain.Enums;

namespace VaryoCms.Application.Interfaces;

// Scoped, per-request. Resolved from the authenticated principal's claims (implemented in Web).
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    UserRole? Role { get; }
    bool IsAdmin { get; }   // TenantAdmin or SystemAdmin — bypasses per-content-type permission checks

    int? TenantId { get; }              // the tenant this session belongs to (null for a SystemAdmin)
    bool IsSystemAdmin { get; }         // logged in via the platform console (system_admins)
    int? ImpersonatedTenantId { get; }  // set while a SystemAdmin is impersonating a tenant
    bool IsImpersonating { get; }
}
