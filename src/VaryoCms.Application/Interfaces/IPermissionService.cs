using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Navigation;

namespace VaryoCms.Application.Interfaces;

// Resolves per-content-type access for the current user. Admins (TenantAdmin/SystemAdmin) get full access.
public interface IPermissionService
{
    // Published content types the current user may read (for the left menu).
    Task<IReadOnlyList<AccessibleContentTypeDto>> GetAccessibleContentTypesAsync(CancellationToken ct = default);

    // Whether the current user may perform the given action on the content type.
    Task<bool> HasPermissionAsync(int contentTypeId, ContentPermission permission, CancellationToken ct = default);

    // All CRUD flags for the content type in one call (for showing/hiding UI actions).
    Task<ContentPermissionSet> GetPermissionsAsync(int contentTypeId, CancellationToken ct = default);
}
