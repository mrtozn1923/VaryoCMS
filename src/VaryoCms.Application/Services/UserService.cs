using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.User;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;
using VaryoCms.Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUserPermissionRepository _permissions;
    private readonly IContentTypeRepository _contentTypes;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IStringLocalizer<SharedResource> _t;
    private readonly IAuditLogger _audit;

    public UserService(
        IUserRepository users,
        IUserPermissionRepository permissions,
        IContentTypeRepository contentTypes,
        IPasswordHasher passwordHasher,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IStringLocalizer<SharedResource> t,
        IAuditLogger audit)
    {
        _users = users;
        _permissions = permissions;
        _contentTypes = contentTypes;
        _passwordHasher = passwordHasher;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _t = t;
        _audit = audit;
    }

    public async Task<Result<PagedResult<UserListItemDto>>> GetListAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var (items, total) = await _users.GetPagedAsync(page, pageSize, ct);
        IReadOnlyList<UserListItemDto> dtos = items.Select(u => new UserListItemDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToList();

        return Result<PagedResult<UserListItemDto>>.Success(
            new PagedResult<UserListItemDto>(dtos, page, pageSize, total));
    }

    public async Task<Result<UserEditDto>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct);
        return user is null
            ? Result<UserEditDto>.Failure(_t["Err.UserNotFound"])
            : Result<UserEditDto>.Success(new UserEditDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = user.IsActive
            });
    }

    public async Task<Result<int>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result<int>.Failure(FirstError(validation));

        // SystemAdmin is a separate entity (system_admins table). Tenant user management cannot create them.
        if (request.Role == UserRole.SystemAdmin)
            return Result<int>.Failure(_t["Validation.Role"]);

        if (await _users.EmailExistsAsync(request.Email, excludeId: null, ct))
            return Result<int>.Failure(_t["Err.EmailInUse", request.Email]);

        var entity = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Role = request.Role,
            IsActive = request.IsActive,
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        int id = await _users.CreateAsync(entity, ct);
        await _audit.LogAsync(AuditActions.UserCreated, "User", id, entityName: entity.Email, ct: ct);
        return Result<int>.Success(id);
    }

    public async Task<Result> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result.Failure(FirstError(validation));

        if (request.Role == UserRole.SystemAdmin)
            return Result.Failure(_t["Validation.Role"]);

        var existing = await _users.GetByIdAsync(id, ct);
        if (existing is null) return Result.Failure(_t["Err.UserNotFound"]);

        if (await _users.EmailExistsAsync(request.Email, excludeId: id, ct))
            return Result.Failure(_t["Err.EmailInUse", request.Email]);

        existing.Email = request.Email;
        existing.FullName = request.FullName;
        existing.Role = request.Role;
        existing.IsActive = request.IsActive;
        await _users.UpdateAsync(existing, ct);

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            await _users.UpdatePasswordAsync(id, _passwordHasher.Hash(request.NewPassword), ct);

        await _audit.LogAsync(AuditActions.UserUpdated, "User", id, entityName: existing.Email, ct: ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await _users.GetByIdAsync(id, ct);
        bool deleted = await _users.SoftDeleteAsync(id, ct);
        if (!deleted) return Result.Failure(_t["Err.UserNotFound"]);
        await _audit.LogAsync(AuditActions.UserDeleted, "User", id, entityName: existing?.Email, ct: ct);
        return Result.Success();
    }

    public async Task<Result<UserPermissionsDto>> GetPermissionsAsync(int userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result<UserPermissionsDto>.Failure(_t["Err.UserNotFound"]);

        var contentTypes = await _contentTypes.GetAllAsync(ct);
        var existing = (await _permissions.GetByUserAsync(userId, ct))
            .ToDictionary(p => p.ContentTypeId);

        IReadOnlyList<ContentTypePermissionDto> rows = contentTypes.Select(ct2 =>
        {
            existing.TryGetValue(ct2.Id, out var p);
            return new ContentTypePermissionDto
            {
                ContentTypeId = ct2.Id,
                ContentTypeName = ct2.Name,
                CanRead = p?.CanRead ?? false,
                CanCreate = p?.CanCreate ?? false,
                CanUpdate = p?.CanUpdate ?? false,
                CanDelete = p?.CanDelete ?? false
            };
        }).ToList();

        return Result<UserPermissionsDto>.Success(new UserPermissionsDto
        {
            UserId = user.Id,
            UserEmail = user.Email,
            Permissions = rows
        });
    }

    public async Task<Result> SavePermissionsAsync(
        int userId, SaveUserPermissionsRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Failure(_t["Err.UserNotFound"]);

        // Only persist rows that grant at least one permission.
        IReadOnlyList<UserContentTypePermission> toSave = request.Permissions
            .Where(p => p.CanRead || p.CanCreate || p.CanUpdate || p.CanDelete)
            .Select(p => new UserContentTypePermission
            {
                UserId = userId,
                ContentTypeId = p.ContentTypeId,
                CanRead = p.CanRead,
                CanCreate = p.CanCreate,
                CanUpdate = p.CanUpdate,
                CanDelete = p.CanDelete
            }).ToList();

        await _permissions.ReplaceForUserAsync(userId, toSave, ct);
        await _audit.LogAsync(AuditActions.UserPermissionsUpdated, "User", userId, entityName: user.Email, ct: ct);
        return Result.Success();
    }

    private static string FirstError(FluentValidation.Results.ValidationResult result)
        => result.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
}
