using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Audit;

namespace VaryoCms.Web.ViewModels;

public class AuditLogListViewModel
{
    public IReadOnlyList<AuditLogDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public string? FilterAction { get; set; }
    public string? FilterEntityType { get; set; }
    public int? FilterContentTypeId { get; set; }
    public DateTime? FilterDateFrom { get; set; }
    public DateTime? FilterDateTo { get; set; }
}
