using System.Data.Common;
using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class UserPermissionRepository : BaseRepository, IUserPermissionRepository
{
    private readonly ITenantContext _tenantContext;

    public UserPermissionRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<IReadOnlyList<UserContentTypePermission>> GetByUserAsync(
        int userId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT id, tenant_id, user_id, content_type_id, can_read, can_create, can_update, can_delete
              FROM user_content_type_permissions
              WHERE user_id = @UserId AND tenant_id = @TenantId",
            new { UserId = userId, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<UserContentTypePermission>(command);
        return rows.AsList();
    }

    public async Task ReplaceForUserAsync(
        int userId, IReadOnlyList<UserContentTypePermission> permissions, CancellationToken ct = default)
    {
        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        var deleteCmd = new CommandDefinition(
            @"DELETE FROM user_content_type_permissions
              WHERE user_id = @UserId AND tenant_id = @TenantId",
            new { UserId = userId, TenantId = _tenantContext.TenantId },
            transaction: tx, cancellationToken: ct);
        await conn.ExecuteAsync(deleteCmd);

        foreach (var p in permissions)
        {
            var insertCmd = new CommandDefinition(
                @"INSERT INTO user_content_type_permissions
                      (tenant_id, user_id, content_type_id, can_read, can_create, can_update, can_delete)
                  VALUES (@TenantId, @UserId, @ContentTypeId, @CanRead, @CanCreate, @CanUpdate, @CanDelete)",
                new
                {
                    TenantId = _tenantContext.TenantId,
                    UserId = userId,
                    p.ContentTypeId,
                    p.CanRead,
                    p.CanCreate,
                    p.CanUpdate,
                    p.CanDelete
                },
                transaction: tx, cancellationToken: ct);
            await conn.ExecuteAsync(insertCmd);
        }

        await tx.CommitAsync(ct);
    }
}
