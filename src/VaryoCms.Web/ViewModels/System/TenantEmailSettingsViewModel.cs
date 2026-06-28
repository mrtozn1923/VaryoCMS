using VaryoCms.Domain.Entities;

namespace VaryoCms.Web.ViewModels.System;

public class TenantEmailSettingsViewModel
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public bool EmailVerificationEnabled { get; set; }
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; }
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Varyo CMS";
    public int CodeExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 3;

    public static TenantEmailSettingsViewModel FromEntity(TenantEmailSettings s, string tenantName) => new()
    {
        TenantId = s.TenantId, TenantName = tenantName,
        EmailVerificationEnabled = s.EmailVerificationEnabled,
        SmtpHost = s.SmtpHost, SmtpPort = s.SmtpPort, SmtpUseSsl = s.SmtpUseSsl,
        SmtpUser = s.SmtpUser, SmtpPassword = s.SmtpPassword,
        FromAddress = s.FromAddress, FromName = s.FromName,
        CodeExpiryMinutes = s.CodeExpiryMinutes, MaxAttempts = s.MaxAttempts
    };

    public TenantEmailSettings ToEntity() => new()
    {
        TenantId = TenantId, EmailVerificationEnabled = EmailVerificationEnabled,
        SmtpHost = SmtpHost?.Trim() ?? "", SmtpPort = SmtpPort, SmtpUseSsl = SmtpUseSsl,
        SmtpUser = SmtpUser?.Trim() ?? "", SmtpPassword = SmtpPassword ?? "",
        FromAddress = FromAddress?.Trim() ?? "", FromName = FromName?.Trim() ?? "Varyo CMS",
        CodeExpiryMinutes = CodeExpiryMinutes < 1 ? 1 : CodeExpiryMinutes,
        MaxAttempts = MaxAttempts < 1 ? 1 : MaxAttempts
    };
}
