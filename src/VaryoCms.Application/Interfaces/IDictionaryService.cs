using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Dictionary;
using VaryoCms.Application.DTOs.Localization;

namespace VaryoCms.Application.Interfaces;

public interface IDictionaryService
{
    Task<Result<PagedResult<DictionaryEntryListItemDto>>> GetListAsync(
        string? search, string? category, int page, int pageSize, CancellationToken ct = default);
    Task<Result<DictionaryEntryEditDto>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveDictionaryEntryRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, SaveDictionaryEntryRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<LanguageDto>> GetActiveLanguagesAsync(CancellationToken ct = default);
}
