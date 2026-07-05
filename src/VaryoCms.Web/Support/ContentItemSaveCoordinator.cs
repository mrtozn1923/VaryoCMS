using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;

namespace VaryoCms.Web.Support;

// Saves a multi-language ContentItemFormViewModel by looping the tenant's active languages and
// calling the existing single-language IContentItemService.CreateAsync/UpdateAsync once per
// language — the Application-layer save contract is untouched. Sequential (not parallel): avoids
// concurrent writes to the same item and keeps the per-language MERGE upserts deterministic.
public class ContentItemSaveCoordinator
{
    private readonly IContentItemService _items;
    private readonly ILanguageService _languages;

    public ContentItemSaveCoordinator(IContentItemService items, ILanguageService languages)
    {
        _items = items;
        _languages = languages;
    }

    public async Task<ContentItemSaveResult> CreateNewAsync(ContentItemFormViewModel vm, CancellationToken ct)
    {
        var languageCodes = await ActiveLanguagesAsync(vm, ct);

        var created = await _items.CreateAsync(vm.ToSaveRequest(vm.PrimaryLanguageCode), ct);
        if (!created.IsSuccess) return ContentItemSaveResult.Failure(vm.PrimaryLanguageCode, created.Error!);

        int itemId = created.Value;
        foreach (var lang in languageCodes)
        {
            if (lang == vm.PrimaryLanguageCode || !vm.HasContent(lang)) continue;

            var updated = await _items.UpdateAsync(itemId, vm.ToSaveRequest(lang), ct);
            if (!updated.IsSuccess) return ContentItemSaveResult.PartialFailure(itemId, lang, updated.Error!);
        }
        return ContentItemSaveResult.Success(itemId);
    }

    public async Task<ContentItemSaveResult> SaveExistingAsync(int itemId, ContentItemFormViewModel vm, CancellationToken ct)
    {
        var languageCodes = await ActiveLanguagesAsync(vm, ct);

        foreach (var lang in languageCodes)
        {
            // Skip a secondary language nobody has touched — otherwise a required localized field
            // that simply hasn't been translated yet would block saving every OTHER language too.
            if (!vm.HasContent(lang)) continue;

            var updated = await _items.UpdateAsync(itemId, vm.ToSaveRequest(lang), ct);
            if (!updated.IsSuccess) return ContentItemSaveResult.PartialFailure(itemId, lang, updated.Error!);
        }
        return ContentItemSaveResult.Success(itemId);
    }

    private async Task<List<string>> ActiveLanguagesAsync(ContentItemFormViewModel vm, CancellationToken ct)
    {
        var active = await _languages.GetActiveAsync(ct);
        var codes = active.Count > 0 ? active.Select(l => l.Code).ToList() : new List<string> { vm.PrimaryLanguageCode };
        if (!codes.Contains(vm.PrimaryLanguageCode)) codes.Insert(0, vm.PrimaryLanguageCode);
        return codes;
    }
}

public class ContentItemSaveResult
{
    public bool IsSuccess { get; private init; }
    public int ItemId { get; private init; }
    public string? FailedLanguage { get; private init; }
    public string? Error { get; private init; }

    // True once the item exists in the DB (create succeeded, or this was already an edit) — on
    // failure the controller must redirect to Edit, not Index, so a partially-saved item isn't lost
    // and a retry doesn't create a duplicate.
    public bool ItemPersisted { get; private init; }

    public static ContentItemSaveResult Success(int itemId) =>
        new() { IsSuccess = true, ItemId = itemId, ItemPersisted = true };

    public static ContentItemSaveResult Failure(string lang, string error) =>
        new() { IsSuccess = false, FailedLanguage = lang, Error = error, ItemPersisted = false };

    public static ContentItemSaveResult PartialFailure(int itemId, string lang, string error) =>
        new() { IsSuccess = false, ItemId = itemId, FailedLanguage = lang, Error = error, ItemPersisted = true };
}
