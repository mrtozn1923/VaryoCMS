namespace VaryoCms.Application.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string? entityType = null,
        int? entityId = null,
        int? contentTypeId = null,
        string? entityName = null,
        object? metadata = null,
        string? userEmailOverride = null,
        int? userIdOverride = null,
        int? tenantIdOverride = null,
        CancellationToken ct = default);
}
