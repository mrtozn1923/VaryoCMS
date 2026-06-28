using VaryoCms.Application.DTOs.Dashboard;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dash;
    private readonly IAuditLogService _audit;

    public DashboardService(IDashboardRepository dash, IAuditLogService audit)
    {
        _dash  = dash;
        _audit = audit;
    }

    public async Task<TenantDashboardDto> GetTenantDashboardAsync(CancellationToken ct = default)
    {
        var counts  = await _dash.GetCountsAsync(ct);
        var recent  = await _audit.GetRecentAsync(10, ct);

        return new TenantDashboardDto
        {
            ContentTypeCount = counts.ContentTypes,
            ContentItemCount = counts.ContentItems,
            MediaAssetCount  = counts.MediaAssets,
            UserCount        = counts.Users,
            LanguageCount    = counts.Languages,
            RecentActivities = recent.IsSuccess ? recent.Value! : [],
        };
    }
}
