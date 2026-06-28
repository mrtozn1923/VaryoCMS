namespace VaryoCms.Application.DTOs.System;

public class SystemDashboardDto
{
    public IReadOnlyList<TenantListItemDto> Tenants { get; set; } = new List<TenantListItemDto>();
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
}
