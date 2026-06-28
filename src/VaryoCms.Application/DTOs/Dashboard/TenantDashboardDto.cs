using VaryoCms.Application.DTOs.Audit;

namespace VaryoCms.Application.DTOs.Dashboard;

public class TenantDashboardDto
{
    public int ContentTypeCount { get; set; }
    public int ContentItemCount { get; set; }
    public int MediaAssetCount { get; set; }
    public int UserCount { get; set; }
    public int LanguageCount { get; set; }
    public IReadOnlyList<AuditLogDto> RecentActivities { get; set; } = [];
}
