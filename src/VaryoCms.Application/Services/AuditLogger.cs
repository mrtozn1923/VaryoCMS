using System.Text.Json;
using Microsoft.Extensions.Logging;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Application.Services;

public class AuditLogger : IAuditLogger
{
    private readonly IAuditLogRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserContext _user;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(
        IAuditLogRepository repo,
        ITenantContext tenant,
        ICurrentUserContext user,
        ILogger<AuditLogger> logger)
    {
        _repo   = repo;
        _tenant = tenant;
        _user   = user;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string? entityType = null,
        int? entityId = null,
        int? contentTypeId = null,
        string? entityName = null,
        object? metadata = null,
        string? userEmailOverride = null,
        int? userIdOverride = null,
        int? tenantIdOverride = null,
        CancellationToken ct = default)
    {
        try
        {
            int tenantId = tenantIdOverride ?? _tenant.TenantId;
            // System-level operations may have no resolved tenant — skip audit rather than violate FK.
            if (tenantId <= 0) return;

            var entry = new AuditLog
            {
                TenantId      = tenantId,
                UserId        = userIdOverride ?? _user.UserId,
                UserEmail     = userEmailOverride ?? _user.Email,
                UserRole      = _user.Role?.ToString(),
                Action        = action,
                EntityType    = entityType,
                EntityId      = entityId,
                ContentTypeId = contentTypeId,
                EntityName    = entityName,
                MetadataJson  = metadata is not null
                    ? JsonSerializer.Serialize(metadata)
                    : null,
            };

            await _repo.InsertAsync(entry, ct);
        }
        catch (Exception ex)
        {
            // Audit failure must not break the main operation.
            _logger.LogError(ex, "Failed to write audit log entry for action {Action}", action);
        }
    }
}
