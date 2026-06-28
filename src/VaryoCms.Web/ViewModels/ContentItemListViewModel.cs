using VaryoCms.Application.DTOs.ContentItem;
using VaryoCms.Application.DTOs.Localization;

namespace VaryoCms.Web.ViewModels;

public class ContentItemListViewModel
{
    public int ContentTypeId { get; set; }
    public string ContentTypeName { get; set; } = string.Empty;
    public IReadOnlyList<ContentItemListItemDto> Items { get; set; } = Array.Empty<ContentItemListItemDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }

    // Current user's permissions on this content type (drive which action buttons are shown).
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }

    // Active filter values (preserved across pagination links).
    public string? Q { get; set; }
    public string? StatusFilter { get; set; }
    public string? LanguageFilter { get; set; }

    // Tenant's active languages (populates the language filter dropdown).
    public IReadOnlyList<LanguageDto> AvailableLanguages { get; set; } = Array.Empty<LanguageDto>();
}
