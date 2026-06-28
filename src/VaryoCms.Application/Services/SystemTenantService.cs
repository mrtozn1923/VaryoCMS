using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.System;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class SystemTenantService : ISystemTenantService
{
    private readonly ITenantProvisioningRepository _tenants;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateTenantRequest> _createValidator;
    private readonly IValidator<UpdateTenantRequest> _updateValidator;
    private readonly IStringLocalizer<SharedResource> _t;
    private readonly IAuditLogger _audit;

    public SystemTenantService(
        ITenantProvisioningRepository tenants,
        IPasswordHasher passwordHasher,
        IValidator<CreateTenantRequest> createValidator,
        IValidator<UpdateTenantRequest> updateValidator,
        IStringLocalizer<SharedResource> t,
        IAuditLogger audit)
    {
        _tenants = tenants;
        _passwordHasher = passwordHasher;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _t = t;
        _audit = audit;
    }

    public async Task<SystemDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var (items, total) = await _tenants.GetPagedAsync(1, 100, ct);
        var dtos = items.Select(Map).ToList();
        return new SystemDashboardDto
        {
            Tenants = dtos,
            TotalTenants = total,
            ActiveTenants = dtos.Count(t => t.IsActive)
        };
    }

    public async Task<Result<PagedResult<TenantListItemDto>>> GetListAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var (items, total) = await _tenants.GetPagedAsync(page, pageSize, ct);
        IReadOnlyList<TenantListItemDto> dtos = items.Select(Map).ToList();
        return Result<PagedResult<TenantListItemDto>>.Success(
            new PagedResult<TenantListItemDto>(dtos, page, pageSize, total));
    }

    public async Task<Result<int>> CreateAsync(CreateTenantRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result<int>.Failure(FirstError(validation));

        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await _tenants.SlugExistsAsync(slug, excludeId: null, ct))
            return Result<int>.Failure(_t["Err.SlugInUse", slug]);

        var data = new NewTenant(
            Name: request.Name.Trim(),
            Slug: slug,
            LanguageCode: request.DefaultLanguageCode.Trim().ToLowerInvariant(),
            LanguageName: request.DefaultLanguageName.Trim(),
            AdminEmail: request.FirstAdminEmail.Trim(),
            AdminPasswordHash: _passwordHasher.Hash(request.FirstAdminPassword),
            AdminFullName: string.IsNullOrWhiteSpace(request.FirstAdminFullName) ? null : request.FirstAdminFullName.Trim());

        int id = await _tenants.ProvisionAsync(data, ct);
        await _audit.LogAsync(AuditActions.SystemTenantCreated, "Tenant", id,
            entityName: data.Name, tenantIdOverride: id, ct: ct);
        return Result<int>.Success(id);
    }

    public async Task<Result<TenantEditDto>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var tenant = await _tenants.GetByIdAsync(id, ct);
        return tenant is null
            ? Result<TenantEditDto>.Failure(_t["Err.TenantNotFound"])
            : Result<TenantEditDto>.Success(new TenantEditDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Slug = tenant.Slug,
                IsActive = tenant.IsActive
            });
    }

    public async Task<Result> UpdateAsync(int id, UpdateTenantRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result.Failure(FirstError(validation));

        bool updated = await _tenants.UpdateAsync(id, request.Name.Trim(), request.IsActive, ct);
        if (!updated) return Result.Failure(_t["Err.TenantNotFound"]);
        await _audit.LogAsync(AuditActions.SystemTenantUpdated, "Tenant", id,
            entityName: request.Name.Trim(), tenantIdOverride: id, ct: ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var tenant = await _tenants.GetByIdAsync(id, ct);
        bool deleted = await _tenants.SoftDeleteAsync(id, ct);
        if (!deleted) return Result.Failure(_t["Err.TenantNotFound"]);
        await _audit.LogAsync(AuditActions.SystemTenantDeleted, "Tenant", id,
            entityName: tenant?.Name, tenantIdOverride: id, ct: ct);
        return Result.Success();
    }

    private static TenantListItemDto Map(TenantSummary t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Slug = t.Slug,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UserCount = t.UserCount,
        ContentTypeCount = t.ContentTypeCount
    };

    private static string FirstError(FluentValidation.Results.ValidationResult result)
        => result.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
}
