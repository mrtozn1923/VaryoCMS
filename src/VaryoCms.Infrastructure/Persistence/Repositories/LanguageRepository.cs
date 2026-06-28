using System.Data.Common;
using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class LanguageRepository : BaseRepository, ILanguageRepository
{
    private const string Columns = "id, tenant_id, code, name, is_default, is_active, flag_icon";

    private readonly ITenantContext _tenantContext;

    public LanguageRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<IReadOnlyList<Language>> GetActiveAsync(CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM languages
               WHERE tenant_id = @TenantId AND is_active = 1
               ORDER BY is_default DESC, name ASC",
            new { TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return (await conn.QueryAsync<Language>(command)).AsList();
    }

    public async Task<IReadOnlyList<Language>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM languages
               WHERE tenant_id = @TenantId
               ORDER BY is_default DESC, is_active DESC, name ASC",
            new { TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return (await conn.QueryAsync<Language>(command)).AsList();
    }

    public async Task<Language?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM languages WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<Language>(command);
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT COUNT(1) FROM languages
              WHERE tenant_id = @TenantId AND code = @Code AND (@ExcludeId IS NULL OR id <> @ExcludeId)",
            new { TenantId = _tenantContext.TenantId, Code = code, ExcludeId = excludeId },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command) > 0;
    }

    public async Task<int> ActiveCountAsync(CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            "SELECT COUNT(1) FROM languages WHERE tenant_id = @TenantId AND is_active = 1",
            new { TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task<int> CreateAsync(Language entity, CancellationToken ct = default)
    {
        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        if (entity.IsDefault)
            await ClearDefaultsAsync(conn, tx, ct);

        int id = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"INSERT INTO languages (tenant_id, code, name, is_default, is_active, flag_icon)
              OUTPUT INSERTED.id
              VALUES (@TenantId, @Code, @Name, @IsDefault, @IsActive, @FlagIcon)",
            new
            {
                TenantId = _tenantContext.TenantId,
                entity.Code,
                entity.Name,
                entity.IsDefault,
                IsActive = entity.IsDefault || entity.IsActive,   // a default language must be active
                entity.FlagIcon
            },
            transaction: tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task<bool> UpdateAsync(Language entity, CancellationToken ct = default)
    {
        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        if (entity.IsDefault)
            await ClearDefaultsAsync(conn, tx, ct);

        int affected = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE languages
              SET name = @Name, is_default = @IsDefault, is_active = @IsActive, flag_icon = @FlagIcon
              WHERE id = @Id AND tenant_id = @TenantId",
            new
            {
                entity.Id,
                TenantId = _tenantContext.TenantId,
                entity.Name,
                entity.IsDefault,
                IsActive = entity.IsDefault || entity.IsActive,   // a default language must be active
                entity.FlagIcon
            },
            transaction: tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return affected > 0;
    }

    public async Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE languages SET is_active = @IsActive
              WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, IsActive = isActive, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    private async Task ClearDefaultsAsync(DbConnection conn, DbTransaction tx, CancellationToken ct)
        => await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE languages SET is_default = 0 WHERE tenant_id = @TenantId AND is_default = 1",
            new { TenantId = _tenantContext.TenantId },
            transaction: tx, cancellationToken: ct));
}
