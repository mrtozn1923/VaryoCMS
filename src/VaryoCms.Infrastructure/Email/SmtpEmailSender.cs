using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;

namespace VaryoCms.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, EmailVerificationSettings settings, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        var socketOptions = settings.SmtpUseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, socketOptions, ct);

        if (!string.IsNullOrEmpty(settings.SmtpUser))
            await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
