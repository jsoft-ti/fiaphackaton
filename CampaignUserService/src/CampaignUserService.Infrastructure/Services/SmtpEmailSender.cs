using System.Net;
using System.Net.Mail;
using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CampaignUserService.Infrastructure.Services;

/// <summary>
/// SMTP-backed email sender. When <see cref="SmtpSettings.Enabled"/> is
/// false (the default, since no real mailbox is configured for this
/// service), messages are logged instead of dispatched, which keeps every
/// flow (registration, password recovery) fully functional end-to-end
/// without requiring live SMTP credentials. Flip the flag and provide
/// credentials via environment variables to enable real delivery - no
/// Application-layer code needs to change.
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpSettings> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpSettings _settings = options.Value;

    public Task SendPasswordResetEmailAsync(
        string toEmail,
        string recipientName,
        string rawResetToken,
        CancellationToken cancellationToken)
    {
        var resetUrl = string.Format(_settings.PasswordResetUrlTemplate, rawResetToken);
        var subject = "Recuperação de senha - Campanhas Sociais";
        var body = $"""
            Olá {recipientName},

            Recebemos uma solicitação de redefinição de senha para sua conta.
            Utilize o link abaixo (válido por 1 hora) para definir uma nova senha:

            {resetUrl}

            Se você não solicitou esta alteração, ignore este email.
            """;

        return SendAsync(toEmail, subject, body, cancellationToken);
    }

    public Task SendWelcomeEmailAsync(string toEmail, string recipientName, CancellationToken cancellationToken)
    {
        var subject = "Bem-vindo(a) à plataforma Campanhas Sociais";
        var body = $"""
            Olá {recipientName},

            Sua conta foi criada com sucesso. Seja bem-vindo(a)!
            """;

        return SendAsync(toEmail, subject, body, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation(
                "[EMAIL - SMTP DISABLED] To: {ToEmail} | Subject: {Subject}\n{Body}",
                toEmail,
                subject,
                body);
            return;
        }

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = _settings.EnableSsl
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromAddress, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Email delivery must never break the caller's business flow (e.g. registration).
            logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
        }
    }
}
