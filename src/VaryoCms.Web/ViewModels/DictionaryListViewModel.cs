using VaryoCms.Application.DTOs.Dictionary;

namespace VaryoCms.Web.ViewModels;

public class DictionaryListViewModel
{
    public IReadOnlyList<DictionaryEntryListItemDto> Items { get; set; } = Array.Empty<DictionaryEntryListItemDto>();
    public string? Search { get; set; }
    public string? Category { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public int LanguageCount { get; set; }   // active languages, to render "x / total" in the list
}
