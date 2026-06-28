using System.Data.Common;
using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

// Admin-side credential access. Tenant scope applied via ITenantContext (same pattern as ApiConfigurationRepository).
public class ApiCredentialRepository : BaseRepository, IApiCredentialRepository
{
    private readonly ITenantContext _tenantContext;

    public ApiCredentialRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<IReadOnlyList<(ApiCredential Credential, int GrantedCount)>> GetAllAsync(
        CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT c.id, c.tenant_id, c.name, c.auth_type, c.api_key, c.is_active,
                     c.created_at, c.updated_at, c.is_deleted,
                     (SELECT COUNT(*) FROM api_credential_content_types j
                      WHERE j.api_credential_id = c.id AND j.tenant_id = @TenantId) AS GrantedCount
              FROM api_credentials c
              WHERE c.tenant_id = @TenantId AND c.is_deleted = 0
              ORDER BY c.created_at DESC",
            new { TenantId = _tenantContext.TenantId },
            cancellationToken: ct);

        var rows = await conn.QueryAsync<ApiCredential, int, (ApiCredential, int)>(
            command, (cred, count) => (cred, count), splitOn: "GrantedCount");
        return rows.AsList();
    }

    public async Task<(ApiCredential Credential, IReadOnlyList<int> ContentTypeIds)?> GetByIdAsync(
        int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();

        var credCmd = new CommandDefinition(
            @"SELECT id, tenant_id, name, auth_type, api_key, is_active, created_at, updated_at, is_deleted
              FROM api_credentials
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        ApiCredential? cred = await conn.QueryFirstOrDefaultAsync<ApiCredential>(credCmd);
        if (cred is null) return null;

        var junctionCmd = new CommandDefinition(
            @"SELECT content_type_id FROM api_credential_content_types
              WHERE api_credential_id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        IEnumerable<int> contentTypeIds = await conn.QueryAsync<int>(junctionCmd);
        return (cred, contentTypeIds.AsList());
    }

    public async Task<int> CreateAsync(ApiCredential credential, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"INSERT INTO api_credentials (tenant_id, name, auth_type, is_active, created_at, updated_at, is_deleted)
              OUTPUT INSERTED.id
              VALUES (@TenantId, @Name, @AuthType, @IsActive, GETUTCDATE(), GETUTCDATE(), 0)",
            new
            {
                TenantId = _tenantContext.TenantId,
                credential.Name,
                AuthType = credential.AuthType.ToString(),  // NVARCHAR column — enum rule
                credential.IsActive
            },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task UpdateAsync(ApiCredential credential, CancellationToken ct = default)
    {
        // auth_type is immutable after creation — not updated here.
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE api_credentials
              SET name = @Name, is_active = @IsActive, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = credential.Id, credential.Name, credential.IsActive, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        await conn.ExecuteAsync(command);
    }

    public async Task UpdateApiKeyAsync(int credentialId, string? apiKeyHash, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE api_credentials SET api_key = @Hash, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = credentialId, Hash = apiKeyHash, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        await conn.ExecuteAsync(command);
    }

    public async Task ReplaceContentTypesAsync(
        int credentialId, IReadOnlyList<int> contentTypeIds, CancellationToken ct = default)
    {
        // Delete-then-insert in a single transaction — mirrors ReplaceFieldVisibilityAsync pattern.
        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        var deleteCmd = new CommandDefinition(
            "DELETE FROM api_credential_content_types WHERE api_credential_id = @Id AND tenant_id = @TenantId",
            new { Id = credentialId, TenantId = _tenantContext.TenantId },
            transaction: tx, cancellationToken: ct);
        await conn.ExecuteAsync(deleteCmd);

        foreach (int ctId in contentTypeIds)
        {
            var insertCmd = new CommandDefinition(
                @"INSERT INTO api_credential_content_types (tenant_id, api_credential_id, content_type_id)
                  VALUES (@TenantId, @CredentialId, @ContentTypeId)",
                new { TenantId = _tenantContext.TenantId, CredentialId = credentialId, ContentTypeId = ctId },
                transaction: tx, cancellationToken: ct);
            await conn.ExecuteAsync(insertCmd);
        }

        await tx.CommitAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE api_credentials SET is_deleted = 1, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        await conn.ExecuteAsync(command);
    }
}
