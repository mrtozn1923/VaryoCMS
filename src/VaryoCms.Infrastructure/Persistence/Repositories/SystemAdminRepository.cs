using Dapper;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

// Cross-tenant: system_admins is a root table, so this repository does NOT depend on ITenantContext.
public class SystemAdminRepository : ISystemAdminRepository
{
    private const string Columns =
        "id, email, password_hash, full_name, is_active, created_at, updated_at, is_deleted";

    private readonly IDbConnectionFactory _connectionFactory;

    public SystemAdminRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<SystemAdmin?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM system_admins
               WHERE email = @Email AND is_deleted = 0",
            new { Email = email },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<SystemAdmin>(command);
    }

    public async Task<SystemAdmin?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM system_admins
               WHERE id = @Id AND is_deleted = 0",
            new { Id = id },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<SystemAdmin>(command);
    }

    public async Task<bool> UpdatePasswordAsync(int id, string passwordHash, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE system_admins SET password_hash = @PasswordHash, updated_at = GETUTCDATE()
              WHERE id = @Id AND is_deleted = 0",
            new { Id = id, PasswordHash = passwordHash },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }
}
