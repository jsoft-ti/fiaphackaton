namespace CampaignUserService.Application.Common.Interfaces;

/// <summary>
/// Abstraction for outbound transactional emails. The current implementation
/// only logs the message (no SMTP credentials in this environment); the
/// interface is ready for a future SMTP-backed implementation to be plugged
/// in via DI without touching any Application code.
/// </summary>
public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(
        string toEmail,
        string recipientName,
        string rawResetToken,
        CancellationToken cancellationToken);

    Task SendWelcomeEmailAsync(string toEmail, string recipientName, CancellationToken cancellationToken);
}
