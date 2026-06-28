using VaryoCms.Application.DTOs.System;

namespace VaryoCms.Web.ViewModels.System;

public class TenantListViewModel
{
    public IReadOnlyList<TenantListItemDto> Items { get; set; } = new List<TenantListItemDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}
