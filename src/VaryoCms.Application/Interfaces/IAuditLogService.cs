using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Audit;

namespace VaryoCms.Application.Interfaces;

public interface IAuditLogService
{
    Task<Result<PagedResult<AuditLogDto>>> GetPagedAsync(
        int page, int pageSize,
        string? action, string? entityType, int? contentTypeId,
        DateTime? dateFrom, DateTime? dateTo,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<AuditLogDto>>> GetRecentForContentTypeAsync(
        int contentTypeId, int take = 10, CancellationToken ct = default);

    Task<Result<IReadOnlyList<AuditLogDto>>> GetRecentAsync(
        int take = 10, CancellationToken ct = default);
}
