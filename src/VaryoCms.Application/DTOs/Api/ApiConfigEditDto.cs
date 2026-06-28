namespace VaryoCms.Application.DTOs.Api;

public class ApiConfigEditDto
{
    public int ContentTypeId { get; set; }
    public string ContentTypeName { get; set; } = null!;
    public string ContentTypeSlug { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public bool IsPublic { get; set; }              // true = no auth required for reads; false = credential required
    public bool AllowFiltering { get; set; } = true;
    public bool AllowSorting { get; set; } = true;
    public bool AllowPagination { get; set; } = true;
    public int RateLimitPerMin { get; set; } = 60;
    public int CacheSeconds { get; set; }
    // Per-verb CRUD permissions (migration 034).
    public bool AllowRead { get; set; } = true;
    public bool AllowCreate { get; set; }
    public bool AllowUpdate { get; set; }
    public bool AllowDelete { get; set; }
    public IReadOnlyList<ApiFieldVisibilityDto> Fields { get; set; } = Array.Empty<ApiFieldVisibilityDto>();
    // Request/response model previews — generated from visible fields in ApiConfigurationService.
    public string? RequestExampleJson { get; set; }
    public string? ResponseExampleJson { get; set; }
}
