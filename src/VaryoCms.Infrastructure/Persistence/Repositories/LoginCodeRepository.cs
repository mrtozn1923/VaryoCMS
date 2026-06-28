using Dapper;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class LoginCodeRepository : BaseRepository, ILoginCodeRepository
{
    public LoginCodeRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task UpsertAsync(string email, string tenantType, int? tenantId, string code, DateTime expiresAt, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        // Delete any existing unused codes for this identity first, then insert fresh.
        await conn.ExecuteAsync(new CommandDefinition(
            @"DELETE FROM login_codes WHERE email = @Email AND tenant_type = @TenantType
              AND (tenant_id = @TenantId OR (@TenantId IS NULL AND tenant_id IS NULL)) AND used_at IS NULL",
            new { Email = email, TenantType = tenantType, TenantId = tenantId },
            cancellationToken: ct));

        await conn.ExecuteAsync(new CommandDefinition(
            @"INSERT INTO login_codes (email, tenant_type, tenant_id, code, attempts, expires_at, created_at)
              VALUES (@Email, @TenantType, @TenantId, @Code, 0, @ExpiresAt, GETUTCDATE())",
            new { Email = email, TenantType = tenantType, TenantId = tenantId, Code = code, ExpiresAt = expiresAt },
            cancellationToken: ct));
    }

    public async Task<LoginCode?> GetActiveAsync(string email, string tenantType, int? tenantId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<LoginCode>(new CommandDefinition(
            @"SELECT TOP 1 id, email, tenant_type, tenant_id, code, attempts, expires_at, used_at, created_at
              FROM login_codes
              WHERE email = @Email AND tenant_type = @TenantType
                AND (tenant_id = @TenantId OR (@TenantId IS NULL AND tenant_id IS NULL))
                AND used_at IS NULL
              ORDER BY created_at DESC",
            new { Email = email, TenantType = tenantType, TenantId = tenantId },
            cancellationToken: ct));
    }

    public async Task<bool> IncrementAttemptsAsync(long id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE login_codes SET attempts = attempts + 1 WHERE id = @Id",
            new { Id = id }, cancellationToken: ct)) > 0;
    }

    public async Task<bool> MarkUsedAsync(long id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE login_codes SET used_at = GETUTCDATE() WHERE id = @Id AND used_at IS NULL",
            new { Id = id }, cancellationToken: ct)) > 0;
    }

    public async Task DeleteByEmailAsync(string email, string tenantType, int? tenantId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            @"DELETE FROM login_codes WHERE email = @Email AND tenant_type = @TenantType
              AND (tenant_id = @TenantId OR (@TenantId IS NULL AND tenant_id IS NULL)) AND used_at IS NULL",
            new { Email = email, TenantType = tenantType, TenantId = tenantId },
            cancellationToken: ct));
    }
}
