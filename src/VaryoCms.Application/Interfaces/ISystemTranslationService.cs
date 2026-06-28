using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.System;

namespace VaryoCms.Application.Interfaces;

// SystemAdmin-only, global admin-UI translation management. Edits invalidate the localizer cache,
// so changes apply to all tenants immediately.
public interface ISystemTranslationService
{
    Task<SystemTranslationListDto> GetListAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<Result<TranslationKeyDto>> GetKeyAsync(string key, CancellationToken ct = default);
    Task<Result> SaveKeyAsync(string key, IReadOnlyDictionary<string, string> values, CancellationToken ct = default);

    Task<IReadOnlyList<UiCultureDto>> GetCulturesAsync(CancellationToken ct = default);
    Task<Result> AddCultureAsync(string code, string name, CancellationToken ct = default);

    Task<string> ExportAsync(string culture, CancellationToken ct = default);            // JSON {key:value}
    Task<Result<int>> ImportAsync(string culture, string json, CancellationToken ct = default);
}
