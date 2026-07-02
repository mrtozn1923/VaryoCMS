using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Application.Services;

// Best-effort "new form submission" email. Resolves per-tenant SMTP settings (falling back to the
// system settings from appsettings), sends to the tenant's configured FromAddress, and swallows all
// errors so a mail/SMTP problem never breaks the public API submission.
public class SubmissionNotifier : ISubmissionNotifier
{
    private readonly ITenantEmailSettingsRepository _tenantEmailSettings;
    private readonly IEmailSender _email;
    private readonly EmailVerificationSettings _systemSettings;
    private readonly ILogger<SubmissionNotifier> _logger;

    public SubmissionNotifier(
        ITenantEmailSettingsRepository tenantEmailSettings, IEmailSender email,
        EmailVerificationSettings systemSettings, ILogger<SubmissionNotifier> logger)
    {
        _tenantEmailSettings = tenantEmailSettings;
        _email = email;
        _systemSettings = systemSettings;
        _logger = logger;
    }

    public async Task NotifyNewSubmissionAsync(
        int tenantId, string contentTypeSlug, string? title,
        IReadOnlyList<(string Label, string Value)> fields, CancellationToken ct = default)
    {
        try
        {
            var settings = await ResolveSettingsAsync(tenantId, ct);

            // SMTP not configured (dev/no mail server) → skip quietly rather than error.
            if (string.IsNullOrWhiteSpace(settings.SmtpHost) || string.IsNullOrWhiteSpace(settings.FromAddress))
            {
                _logger.LogDebug("Submission notify skipped (SMTP/FromAddress not configured) for tenant {TenantId}", tenantId);
                return;
            }

            // Recipient is the tenant's own configured address (site owner's inbox).
            string recipient = settings.FromAddress;
            string subject = $"Yeni form gönderimi: {(!string.IsNullOrWhiteSpace(title) ? title : contentTypeSlug)}";

            await _email.SendAsync(recipient, subject, BuildHtml(contentTypeSlug, title, fields), settings, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Submission notification email failed for tenant {TenantId} ({Slug})", tenantId, contentTypeSlug);
        }
    }

    private async Task<EmailVerificationSettings> ResolveSettingsAsync(int tenantId, CancellationToken ct)
    {
        var ts = await _tenantEmailSettings.GetByTenantIdAsync(tenantId, ct);
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

    private static string BuildHtml(string contentTypeSlug, string? title, IReadOnlyList<(string Label, string Value)> fields)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;font-size:14px;color:#1f2937\">");
        sb.Append("<h2 style=\"margin:0 0 4px\">Yeni form gönderimi</h2>");
        sb.Append($"<p style=\"margin:0 0 16px;color:#6b7280\">İçerik tipi: {WebUtility.HtmlEncode(contentTypeSlug)}");
        if (!string.IsNullOrWhiteSpace(title))
            sb.Append($" · {WebUtility.HtmlEncode(title)}");
        sb.Append("</p><table style=\"border-collapse:collapse;width:100%;max-width:600px\">");
        foreach (var (label, value) in fields)
        {
            sb.Append("<tr>");
            sb.Append($"<td style=\"padding:8px 12px;border:1px solid #e5e7eb;background:#f9fafb;font-weight:600;white-space:nowrap;vertical-align:top\">{WebUtility.HtmlEncode(label)}</td>");
            sb.Append($"<td style=\"padding:8px 12px;border:1px solid #e5e7eb\">{WebUtility.HtmlEncode(value).Replace("\n", "<br>")}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</table></div>");
        return sb.ToString();
    }
}
