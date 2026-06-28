using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task<long> InsertAsync(AuditLog entry, CancellationToken ct = default);
    Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPagedAsync(
        int page, int pageSize,
        string? action, string? entityType, int? contentTypeId,
        DateTime? dateFrom, DateTime? dateTo,
        CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetRecentForContentTypeAsync(int contentTypeId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int take, CancellationToken ct = default);
}
