namespace VaryoCms.Application.DTOs.Api;

public class SaveApiConfigRequest
{
    public int ContentTypeId { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsPublic { get; set; }
    public bool AllowFiltering { get; set; }
    public bool AllowSorting { get; set; }
    public bool AllowPagination { get; set; }
    public int RateLimitPerMin { get; set; }
    public int CacheSeconds { get; set; }
    // Per-verb CRUD permissions (migration 034).
    public bool AllowRead { get; set; } = true;
    public bool AllowCreate { get; set; }
    public bool AllowUpdate { get; set; }
    public bool AllowDelete { get; set; }
    public List<ApiFieldVisibilityDto> Fields { get; set; } = new();
}
