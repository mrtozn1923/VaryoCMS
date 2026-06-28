namespace VaryoCms.Domain.Entities;

public class TenantEmailSettings
{
    public int TenantId { get; set; }
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
    public DateTime UpdatedAt { get; set; }
}
