using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class UserRepository : BaseRepository, IUserRepository
{
    private const string Columns =
        "id, tenant_id, email, password_hash, full_name, role, is_active, created_at, updated_at, is_deleted";

    private readonly ITenantContext _tenantContext;

    public UserRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<(IReadOnlyList<User> Items, int Total)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var args = new
        {
            TenantId = _tenantContext.TenantId,
            Skip = (page - 1) * pageSize,
            Take = pageSize
        };

        var listCmd = new CommandDefinition(
            $@"SELECT {Columns} FROM users
               WHERE tenant_id = @TenantId AND is_deleted = 0
               ORDER BY email ASC
               OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            args, cancellationToken: ct);
        var items = (await conn.QueryAsync<User>(listCmd)).AsList();

        var countCmd = new CommandDefinition(
            @"SELECT COUNT(1) FROM users WHERE tenant_id = @TenantId AND is_deleted = 0",
            args, cancellationToken: ct);
        int total = await conn.ExecuteScalarAsync<int>(countCmd);

        return (items, total);
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM users
               WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<User>(command);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM users
               WHERE email = @Email AND tenant_id = @TenantId AND is_deleted = 0",
            new { Email = email, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<User>(command);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT COUNT(1) FROM users
              WHERE email = @Email AND tenant_id = @TenantId AND is_deleted = 0
                AND (@ExcludeId IS NULL OR id <> @ExcludeId)",
            new { Email = email, TenantId = _tenantContext.TenantId, ExcludeId = excludeId },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command) > 0;
    }

    public async Task<int> CreateAsync(User entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"INSERT INTO users (tenant_id, email, password_hash, full_name, role, is_active)
              OUTPUT INSERTED.id
              VALUES (@TenantId, @Email, @PasswordHash, @FullName, @Role, @IsActive)",
            new
            {
                TenantId = _tenantContext.TenantId,
                entity.Email,
                entity.PasswordHash,
                entity.FullName,
                Role = entity.Role.ToString(),   // enum column is NVARCHAR; write the name explicitly
                entity.IsActive
            },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task<bool> UpdateAsync(User entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE users
              SET email = @Email, full_name = @FullName, role = @Role, is_active = @IsActive,
                  updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new
            {
                entity.Id,
                TenantId = _tenantContext.TenantId,
                entity.Email,
                entity.FullName,
                Role = entity.Role.ToString(),
                entity.IsActive
            },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<bool> UpdatePasswordAsync(int id, string passwordHash, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE users SET password_hash = @PasswordHash, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, PasswordHash = passwordHash, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE users SET is_deleted = 1, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }
}
