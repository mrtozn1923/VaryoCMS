using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Dictionary;
using VaryoCms.Application.DTOs.Localization;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class DictionaryService : IDictionaryService
{
    private readonly IDictionaryRepository _repository;
    private readonly ILanguageRepository _languageRepository;
    private readonly IValidator<SaveDictionaryEntryRequest> _validator;
    private readonly IStringLocalizer<SharedResource> _t;
    private readonly IAuditLogger _audit;

    public DictionaryService(
        IDictionaryRepository repository,
        ILanguageRepository languageRepository,
        IValidator<SaveDictionaryEntryRequest> validator,
        IStringLocalizer<SharedResource> t,
        IAuditLogger audit)
    {
        _repository = repository;
        _languageRepository = languageRepository;
        _validator = validator;
        _t = t;
        _audit = audit;
    }

    public async Task<Result<PagedResult<DictionaryEntryListItemDto>>> GetListAsync(
        string? search, string? category, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var (items, total) = await _repository.GetPagedAsync(
            string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            page, pageSize, ct);

        var counts = items.Count == 0
            ? new Dictionary<int, int>()
            : await _repository.GetTranslatedCountsAsync(items.Select(i => i.Id).ToList(), ct);

        IReadOnlyList<DictionaryEntryListItemDto> dtos = items.Select(e => new DictionaryEntryListItemDto
        {
            Id = e.Id,
            KeyName = e.KeyName,
            Category = e.Category,
            TranslatedCount = counts.TryGetValue(e.Id, out int c) ? c : 0,
            UpdatedAt = e.UpdatedAt
        }).ToList();

        return Result<PagedResult<DictionaryEntryListItemDto>>.Success(
            new PagedResult<DictionaryEntryListItemDto>(dtos, page, pageSize, total));
    }

    public async Task<Result<DictionaryEntryEditDto>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var entry = await _repository.GetByIdAsync(id, ct);
        if (entry is null) return Result<DictionaryEntryEditDto>.Failure(_t["Err.DictionaryNotFound"]);

        var translations = await _repository.GetTranslationsAsync(id, ct);
        return Result<DictionaryEntryEditDto>.Success(new DictionaryEntryEditDto
        {
            Id = entry.Id,
            KeyName = entry.KeyName,
            Category = entry.Category,
            // language_code is CHAR(5) (space-padded); trim so keys match the form field names.
            Translations = translations.ToDictionary(t => t.LanguageCode.Trim(), t => t.Value)
        });
    }

    public async Task<Result<int>> CreateAsync(SaveDictionaryEntryRequest request, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result<int>.Failure(FirstError(validation));

        if (await _repository.KeyExistsAsync(request.KeyName, excludeId: null, ct))
            return Result<int>.Failure(_t["Err.DictionaryKeyInUse", request.KeyName]);

        var entity = new DictionaryEntry { KeyName = request.KeyName, Category = request.Category };
        int id = await _repository.CreateAsync(entity, ct);
        await _repository.SaveTranslationsAsync(id, request.Translations, ct);
        await _audit.LogAsync(AuditActions.DictionaryCreated, "Dictionary", id, entityName: entity.KeyName, ct: ct);
        return Result<int>.Success(id);
    }

    public async Task<Result> UpdateAsync(int id, SaveDictionaryEntryRequest request, CancellationToken ct = default)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result.Failure(FirstError(validation));

        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return Result.Failure(_t["Err.DictionaryNotFound"]);

        if (await _repository.KeyExistsAsync(request.KeyName, excludeId: id, ct))
            return Result.Failure(_t["Err.DictionaryKeyInUse", request.KeyName]);

        existing.KeyName = request.KeyName;
        existing.Category = request.Category;
        await _repository.UpdateAsync(existing, ct);
        await _repository.SaveTranslationsAsync(id, request.Translations, ct);
        await _audit.LogAsync(AuditActions.DictionaryUpdated, "Dictionary", id, entityName: existing.KeyName, ct: ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        bool deleted = await _repository.SoftDeleteAsync(id, ct);
        if (!deleted) return Result.Failure(_t["Err.DictionaryNotFound"]);
        await _audit.LogAsync(AuditActions.DictionaryDeleted, "Dictionary", id, entityName: existing?.KeyName, ct: ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<LanguageDto>> GetActiveLanguagesAsync(CancellationToken ct = default)
    {
        var langs = await _languageRepository.GetActiveAsync(ct);
        return langs.Select(l => new LanguageDto
        {
            Code = l.Code.Trim(),
            Name = l.Name,
            IsDefault = l.IsDefault
        }).ToList();
    }

    private static string FirstError(FluentValidation.Results.ValidationResult result)
        => result.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
}
