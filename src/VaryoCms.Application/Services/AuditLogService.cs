using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Audit;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repo;

    public AuditLogService(IAuditLogRepository repo) => _repo = repo;

    public async Task<Result<PagedResult<AuditLogDto>>> GetPagedAsync(
        int page, int pageSize,
        string? action, string? entityType, int? contentTypeId,
        DateTime? dateFrom, DateTime? dateTo,
        CancellationToken ct = default)
    {
        var (items, total) = await _repo.GetPagedAsync(
            page, pageSize, action, entityType, contentTypeId, dateFrom, dateTo, ct);

        return Result<PagedResult<AuditLogDto>>.Success(
            new PagedResult<AuditLogDto>(items.Select(Map).ToList(), page, pageSize, total));
    }

    public async Task<Result<IReadOnlyList<AuditLogDto>>> GetRecentForContentTypeAsync(
        int contentTypeId, int take = 10, CancellationToken ct = default)
    {
        var items = await _repo.GetRecentForContentTypeAsync(contentTypeId, take, ct);
        return Result<IReadOnlyList<AuditLogDto>>.Success(items.Select(Map).ToList());
    }

    public async Task<Result<IReadOnlyList<AuditLogDto>>> GetRecentAsync(
        int take = 10, CancellationToken ct = default)
    {
        var items = await _repo.GetRecentAsync(take, ct);
        return Result<IReadOnlyList<AuditLogDto>>.Success(items.Select(Map).ToList());
    }

    private static AuditLogDto Map(AuditLog e) => new()
    {
        Id            = e.Id,
        TenantId      = e.TenantId,
        UserId        = e.UserId,
        UserEmail     = e.UserEmail,
        UserRole      = e.UserRole,
        Action        = e.Action,
        EntityType    = e.EntityType,
        EntityId      = e.EntityId,
        ContentTypeId = e.ContentTypeId,
        EntityName    = e.EntityName,
        Description   = e.Description,
        MetadataJson  = e.MetadataJson,
        IpAddress     = e.IpAddress,
        CreatedAt     = e.CreatedAt,
    };
}
