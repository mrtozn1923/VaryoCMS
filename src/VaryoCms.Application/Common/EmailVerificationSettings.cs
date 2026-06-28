namespace VaryoCms.Application.Common;

// Bound from appsettings.json section "EmailVerification".
// Enabled = false → two-factor login is skipped entirely.
public class EmailVerificationSettings
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    // false → STARTTLS (port 587); true → SSL on connect (port 465)
    public bool SmtpUseSsl { get; set; }
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Varyo CMS";
    public int CodeExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 3;
}
