using VaryoCms.Application.DTOs.Api;

namespace VaryoCms.Web.ViewModels;

public class ApiConfigFormViewModel
{
    public int ContentTypeId { get; set; }
    public string ContentTypeName { get; set; } = string.Empty;
    public string ContentTypeSlug { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsPublic { get; set; }           // true = no auth for reads; false = credential required
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
    public List<ApiFieldVisibilityDto> Fields { get; set; } = new();
    // Read-only previews generated server-side — not posted back.
    public string? RequestExampleJson { get; set; }
    public string? ResponseExampleJson { get; set; }

    public SaveApiConfigRequest ToRequest() => new()
    {
        ContentTypeId = ContentTypeId,
        IsEnabled = IsEnabled,
        IsPublic = IsPublic,
        AllowFiltering = AllowFiltering,
        AllowSorting = AllowSorting,
        AllowPagination = AllowPagination,
        RateLimitPerMin = RateLimitPerMin,
        CacheSeconds = CacheSeconds,
        AllowRead = AllowRead,
        AllowCreate = AllowCreate,
        AllowUpdate = AllowUpdate,
        AllowDelete = AllowDelete,
        Fields = Fields ?? new()
    };

    public static ApiConfigFormViewModel FromDto(ApiConfigEditDto d) => new()
    {
        ContentTypeId = d.ContentTypeId,
        ContentTypeName = d.ContentTypeName,
        ContentTypeSlug = d.ContentTypeSlug,
        IsEnabled = d.IsEnabled,
        IsPublic = d.IsPublic,
        AllowFiltering = d.AllowFiltering,
        AllowSorting = d.AllowSorting,
        AllowPagination = d.AllowPagination,
        RateLimitPerMin = d.RateLimitPerMin,
        CacheSeconds = d.CacheSeconds,
        AllowRead = d.AllowRead,
        AllowCreate = d.AllowCreate,
        AllowUpdate = d.AllowUpdate,
        AllowDelete = d.AllowDelete,
        Fields = d.Fields.ToList(),
        RequestExampleJson = d.RequestExampleJson,
        ResponseExampleJson = d.ResponseExampleJson
    };
}
