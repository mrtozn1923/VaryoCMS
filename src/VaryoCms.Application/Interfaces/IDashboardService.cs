using VaryoCms.Application.DTOs.Dashboard;

namespace VaryoCms.Application.Interfaces;

public interface IDashboardService
{
    Task<TenantDashboardDto> GetTenantDashboardAsync(CancellationToken ct = default);
}
