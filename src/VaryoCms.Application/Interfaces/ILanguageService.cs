using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Localization;

namespace VaryoCms.Application.Interfaces;

// Active languages for the current tenant + per-tenant language management (TenantAdmin).
// Tenant scope is applied in the repository.
public interface ILanguageService
{
    Task<IReadOnlyList<LanguageDto>> GetActiveAsync(CancellationToken ct = default);

    Task<IReadOnlyList<LanguageListItemDto>> GetListAsync(CancellationToken ct = default);
    Task<Result<LanguageEditDto>> GetForEditAsync(int id, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(CreateLanguageRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, UpdateLanguageRequest request, CancellationToken ct = default);
    Task<Result> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
}
