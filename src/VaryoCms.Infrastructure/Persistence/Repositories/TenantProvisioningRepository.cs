using System.Data.Common;
using Dapper;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

// Cross-tenant: manages the root `tenants` table, so it does NOT depend on ITenantContext.
public class TenantProvisioningRepository : ITenantProvisioningRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TenantProvisioningRepository(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<(IReadOnlyList<TenantSummary> Items, int Total)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var args = new { Skip = (page - 1) * pageSize, Take = pageSize };

        var listCmd = new CommandDefinition(
            @"SELECT t.id, t.name, t.slug, t.is_active, t.created_at,
                     (SELECT COUNT(1) FROM users u WHERE u.tenant_id = t.id AND u.is_deleted = 0) AS user_count,
                     (SELECT COUNT(1) FROM content_types c WHERE c.tenant_id = t.id AND c.is_deleted = 0) AS content_type_count
              FROM tenants t
              WHERE t.is_deleted = 0
              ORDER BY t.created_at DESC
              OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            args, cancellationToken: ct);
        var items = (await conn.QueryAsync<TenantSummary>(listCmd)).AsList();

        var countCmd = new CommandDefinition(
            "SELECT COUNT(1) FROM tenants WHERE is_deleted = 0", cancellationToken: ct);
        int total = await conn.ExecuteScalarAsync<int>(countCmd);

        return (items, total);
    }

    public async Task<Tenant?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            @"SELECT id, name, slug, is_active, created_at, updated_at, is_deleted
              FROM tenants WHERE id = @Id AND is_deleted = 0",
            new { Id = id }, cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Tenant>(command);
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            @"SELECT COUNT(1) FROM tenants
              WHERE slug = @Slug AND is_deleted = 0 AND (@ExcludeId IS NULL OR id <> @ExcludeId)",
            new { Slug = slug, ExcludeId = excludeId }, cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command) > 0;
    }

    public async Task<int> ProvisionAsync(NewTenant data, CancellationToken ct = default)
    {
        using var conn = (DbConnection)_connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        int tenantId = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"INSERT INTO tenants (name, slug, is_active)
              OUTPUT INSERTED.id
              VALUES (@Name, @Slug, 1)",
            new { data.Name, data.Slug }, transaction: tx, cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO languages (tenant_id, code, name, is_default, is_active)
              VALUES (@TenantId, @LanguageCode, @LangName, 1, 1)",
            new { TenantId = tenantId, data.LanguageCode, LangName = data.LanguageName },
            transaction: tx, cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO users (tenant_id, email, password_hash, full_name, role, is_active)
              VALUES (@TenantId, @Email, @PasswordHash, @FullName, @Role, 1)",
            new
            {
                TenantId = tenantId,
                Email = data.AdminEmail,
                PasswordHash = data.AdminPasswordHash,
                FullName = data.AdminFullName,
                Role = "TenantAdmin"   // enum column is NVARCHAR; write the name explicitly
            },
            transaction: tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return tenantId;
    }

    public async Task<bool> UpdateAsync(int id, string name, bool isActive, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE tenants SET name = @Name, is_active = @IsActive, updated_at = GETUTCDATE()
              WHERE id = @Id AND is_deleted = 0",
            new { Id = id, Name = name, IsActive = isActive }, cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE tenants SET is_deleted = 1, updated_at = GETUTCDATE()
              WHERE id = @Id AND is_deleted = 0",
            new { Id = id }, cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }
}
