namespace VaryoCms.Domain.Interfaces.Repositories;

public interface IContentItemTitleRepository
{
    // Returns (Title, IsActive) for the row, or null if no title exists for that language.
    Task<(string? Title, bool IsActive)?> GetTitleAsync(int itemId, string languageCode, CancellationToken ct = default);

    // Upserts the title with is_active flag; deletes the row when title is null or whitespace.
    Task SaveTitleAsync(int itemId, string languageCode, string? title, bool isActive, CancellationToken ct = default);

    // Returns a mapping of itemId -> (Code, IsActive) pairs for grid language badges.
    Task<IReadOnlyDictionary<int, IReadOnlyList<(string Code, bool IsActive)>>> GetFilledLanguagesAsync(
        IReadOnlyList<int> itemIds, CancellationToken ct = default);
}
