using Dapper;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class TenantEmailSettingsRepository : BaseRepository, ITenantEmailSettingsRepository
{
    public TenantEmailSettingsRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<TenantEmailSettings?> GetByTenantIdAsync(int tenantId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<TenantEmailSettings>(new CommandDefinition(
            @"SELECT tenant_id, email_verification_enabled, smtp_host, smtp_port, smtp_use_ssl,
                     smtp_user, smtp_password, from_address, from_name,
                     code_expiry_minutes, max_attempts, updated_at
              FROM tenant_email_settings WHERE tenant_id = @TenantId",
            new { TenantId = tenantId }, cancellationToken: ct));
    }

    public async Task UpsertAsync(TenantEmailSettings s, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            @"MERGE tenant_email_settings AS t
              USING (SELECT @TenantId AS tenant_id) AS src ON t.tenant_id = src.tenant_id
              WHEN MATCHED THEN UPDATE SET
                email_verification_enabled = @EmailVerificationEnabled,
                smtp_host = @SmtpHost, smtp_port = @SmtpPort, smtp_use_ssl = @SmtpUseSsl,
                smtp_user = @SmtpUser, smtp_password = @SmtpPassword,
                from_address = @FromAddress, from_name = @FromName,
                code_expiry_minutes = @CodeExpiryMinutes, max_attempts = @MaxAttempts,
                updated_at = GETUTCDATE()
              WHEN NOT MATCHED THEN INSERT
                (tenant_id, email_verification_enabled, smtp_host, smtp_port, smtp_use_ssl,
                 smtp_user, smtp_password, from_address, from_name, code_expiry_minutes, max_attempts, updated_at)
              VALUES
                (@TenantId, @EmailVerificationEnabled, @SmtpHost, @SmtpPort, @SmtpUseSsl,
                 @SmtpUser, @SmtpPassword, @FromAddress, @FromName, @CodeExpiryMinutes, @MaxAttempts, GETUTCDATE());",
            new
            {
                s.TenantId, s.EmailVerificationEnabled, s.SmtpHost, s.SmtpPort, s.SmtpUseSsl,
                s.SmtpUser, s.SmtpPassword, s.FromAddress, s.FromName, s.CodeExpiryMinutes, s.MaxAttempts
            },
            cancellationToken: ct));
    }
}
