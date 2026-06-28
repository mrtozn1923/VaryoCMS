using System.Security.Claims;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Enums;

namespace VaryoCms.Web.Contexts;

// Reads the current user's identity from the authenticated principal's claims (set at login).
public class CurrentUserContext : ICurrentUserContext
{
    private readonly ClaimsPrincipal? _principal;

    public CurrentUserContext(IHttpContextAccessor accessor) => _principal = accessor.HttpContext?.User;

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated == true;

    public int? UserId =>
        int.TryParse(_principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public string? Email => _principal?.FindFirst(ClaimTypes.Email)?.Value;

    public string? FullName => _principal?.FindFirst("FullName")?.Value;

    public UserRole? Role =>
        Enum.TryParse<UserRole>(_principal?.FindFirst(ClaimTypes.Role)?.Value, out var role) ? role : null;

    public bool IsAdmin => Role is UserRole.TenantAdmin or UserRole.SystemAdmin;

    public int? TenantId =>
        int.TryParse(_principal?.FindFirst("TenantId")?.Value, out var id) ? id : null;

    public bool IsSystemAdmin => Role is UserRole.SystemAdmin;

    public int? ImpersonatedTenantId =>
        int.TryParse(_principal?.FindFirst("ImpersonatedTenantId")?.Value, out var id) ? id : null;

    public bool IsImpersonating => ImpersonatedTenantId is not null;
}
