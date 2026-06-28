namespace VaryoCms.Application.DTOs.System;

public class SystemTranslationListDto
{
    public IReadOnlyList<UiCultureDto> Cultures { get; set; } = new List<UiCultureDto>();
    public IReadOnlyList<TranslationKeyDto> Keys { get; set; } = new List<TranslationKeyDto>();
    public string? Search { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}
