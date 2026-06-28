using System.Security.Cryptography;
using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Application.Services;

public class LoginCodeService : ILoginCodeService
{
    private readonly ILoginCodeRepository _repo;
    private readonly ITenantEmailSettingsRepository _tenantEmailSettings;
    private readonly IEmailSender _email;
    private readonly EmailVerificationSettings _systemSettings;

    public LoginCodeService(
        ILoginCodeRepository repo,
        ITenantEmailSettingsRepository tenantEmailSettings,
        IEmailSender email,
        EmailVerificationSettings systemSettings)
    {
        _repo = repo;
        _tenantEmailSettings = tenantEmailSettings;
        _email = email;
        _systemSettings = systemSettings;
    }

    public bool IsSystemEnabled => _systemSettings.Enabled;

    public async Task<bool> IsTenantEnabledAsync(int tenantId, CancellationToken ct = default)
    {
        var settings = await _tenantEmailSettings.GetByTenantIdAsync(tenantId, ct);
        return settings?.EmailVerificationEnabled ?? false;
    }

    public async Task SendCodeAsync(string email, string tenantType, int? tenantId, CancellationToken ct = default)
    {
        EmailVerificationSettings settings = tenantType == "system"
            ? _systemSettings
            : await ResolveTenantSettingsAsync(tenantId, ct);

        string code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(settings.CodeExpiryMinutes);

        await _repo.UpsertAsync(email, tenantType, tenantId, code, expiresAt, ct);
        await _email.SendAsync(email, "Varyo CMS — Giriş Doğrulama", BuildEmailHtml(code, settings.CodeExpiryMinutes), settings, ct);
    }

    public async Task<Result> VerifyAsync(string email, string tenantType, int? tenantId, string code, CancellationToken ct = default)
    {
        var record = await _repo.GetActiveAsync(email, tenantType, tenantId, ct);
        if (record is null)
            return Result.Failure("NotFound");

        int maxAttempts = await GetMaxAttemptsAsync(tenantType, tenantId, ct);

        if (record.ExpiresAt < DateTime.UtcNow)
        {
            await _repo.DeleteByEmailAsync(email, tenantType, tenantId, ct);
            return Result.Failure("ExpiredCode");
        }

        if (record.Attempts >= maxAttempts)
        {
            await _repo.DeleteByEmailAsync(email, tenantType, tenantId, ct);
            return Result.Failure("MaxAttemptsExceeded");
        }

        if (record.Code != code.Trim())
        {
            await _repo.IncrementAttemptsAsync(record.Id, ct);
            int remaining = maxAttempts - record.Attempts - 1;
            return Result.Failure($"InvalidCode:{remaining}");
        }

        await _repo.MarkUsedAsync(record.Id, ct);
        return Result.Success();
    }

    private async Task<EmailVerificationSettings> ResolveTenantSettingsAsync(int? tenantId, CancellationToken ct)
    {
        if (tenantId is null) return _systemSettings;
        var ts = await _tenantEmailSettings.GetByTenantIdAsync(tenantId.Value, ct);
        if (ts is null) return _systemSettings;
        return new EmailVerificationSettings
        {
            Enabled = ts.EmailVerificationEnabled,
            SmtpHost = ts.SmtpHost, SmtpPort = ts.SmtpPort, SmtpUseSsl = ts.SmtpUseSsl,
            SmtpUser = ts.SmtpUser, SmtpPassword = ts.SmtpPassword,
            FromAddress = ts.FromAddress, FromName = ts.FromName,
            CodeExpiryMinutes = ts.CodeExpiryMinutes, MaxAttempts = ts.MaxAttempts
        };
    }

    private async Task<int> GetMaxAttemptsAsync(string tenantType, int? tenantId, CancellationToken ct)
    {
        if (tenantType == "system") return _systemSettings.MaxAttempts;
        var ts = tenantId.HasValue ? await _tenantEmailSettings.GetByTenantIdAsync(tenantId.Value, ct) : null;
        return ts?.MaxAttempts ?? _systemSettings.MaxAttempts;
    }

    private static string BuildEmailHtml(string code, int expiryMinutes) =>
        $"""
        <div style="font-family:sans-serif;max-width:480px;margin:0 auto;padding:24px;background:#fff;border:1px solid #e5e7eb;border-radius:8px;">
          <h2 style="color:#312e81;margin-top:0;">Varyo CMS — Giriş Doğrulama</h2>
          <p style="color:#374151;">Giriş doğrulama kodunuz:</p>
          <div style="font-size:40px;font-weight:700;letter-spacing:10px;color:#312e81;padding:16px 0;">{code}</div>
          <p style="color:#6b7280;font-size:14px;">Bu kod <strong>{expiryMinutes} dakika</strong> geçerlidir.</p>
          <p style="color:#6b7280;font-size:12px;">Bu isteği siz yapmadıysanız e-postayı yoksayın.</p>
        </div>
        """;
}
