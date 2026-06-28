using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Navigation;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly ICurrentUserContext _user;
    private readonly IContentTypeRepository _contentTypes;
    private readonly IUserPermissionRepository _permissions;

    public PermissionService(
        ICurrentUserContext user,
        IContentTypeRepository contentTypes,
        IUserPermissionRepository permissions)
    {
        _user = user;
        _contentTypes = contentTypes;
        _permissions = permissions;
    }

    public async Task<IReadOnlyList<AccessibleContentTypeDto>> GetAccessibleContentTypesAsync(
        CancellationToken ct = default)
    {
        if (!_user.IsAuthenticated) return Array.Empty<AccessibleContentTypeDto>();

        var published = (await _contentTypes.GetAllAsync(ct)).Where(c => c.IsPublished).ToList();
        if (_user.IsAdmin) return published.Select(Map).ToList();

        var readable = (await _permissions.GetByUserAsync(_user.UserId!.Value, ct))
            .Where(p => p.CanRead).Select(p => p.ContentTypeId).ToHashSet();
        return published.Where(c => readable.Contains(c.Id)).Select(Map).ToList();
    }

    public async Task<bool> HasPermissionAsync(
        int contentTypeId, ContentPermission permission, CancellationToken ct = default)
    {
        if (!_user.IsAuthenticated || _user.UserId is null) return false;
        if (_user.IsAdmin) return true;

        var row = (await _permissions.GetByUserAsync(_user.UserId.Value, ct))
            .FirstOrDefault(p => p.ContentTypeId == contentTypeId);
        if (row is null) return false;

        return permission switch
        {
            ContentPermission.Read => row.CanRead,
            ContentPermission.Create => row.CanCreate,
            ContentPermission.Update => row.CanUpdate,
            ContentPermission.Delete => row.CanDelete,
            _ => false
        };
    }

    public async Task<ContentPermissionSet> GetPermissionsAsync(int contentTypeId, CancellationToken ct = default)
    {
        if (!_user.IsAuthenticated || _user.UserId is null) return ContentPermissionSet.None;
        if (_user.IsAdmin) return ContentPermissionSet.All;

        var row = (await _permissions.GetByUserAsync(_user.UserId.Value, ct))
            .FirstOrDefault(p => p.ContentTypeId == contentTypeId);
        return row is null
            ? ContentPermissionSet.None
            : new ContentPermissionSet(row.CanRead, row.CanCreate, row.CanUpdate, row.CanDelete);
    }

    private static AccessibleContentTypeDto Map(ContentType c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
        Icon = c.Icon,
        ParentId = c.ParentId
    };
}
