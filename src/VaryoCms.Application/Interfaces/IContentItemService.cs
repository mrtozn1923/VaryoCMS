using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.ContentItem;

namespace VaryoCms.Application.Interfaces;

public interface IContentItemService
{
    Task<Result<PagedResult<ContentItemListItemDto>>> GetListAsync(
        int contentTypeId, string languageCode, int page, int pageSize,
        string? searchQuery = null, string? statusFilter = null, string? languageFilter = null,
        CancellationToken ct = default);
    Task<Result<ContentItemEditDto>> GetForEditAsync(int itemId, string languageCode, CancellationToken ct = default);
    Task<Result<int>> CreateAsync(SaveContentItemRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int itemId, SaveContentItemRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int itemId, CancellationToken ct = default);

    // Searches a target content type's published items for the relation picker.
    Task<Result<IReadOnlyList<RelatedItemDto>>> SearchRelatedAsync(
        int targetContentTypeId, string? displayFieldSlug, string? query, string languageCode, CancellationToken ct = default);
}
