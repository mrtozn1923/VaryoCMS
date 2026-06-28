using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.System;

namespace VaryoCms.Application.Interfaces;

// SystemAdmin-only, cross-tenant tenant management + platform dashboard.
public interface ISystemTenantService
{
    Task<SystemDashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<Result<PagedResult<TenantListItemDto>>> GetListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(CreateTenantRequest request, CancellationToken ct = default);
    Task<Result<TenantEditDto>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, UpdateTenantRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
