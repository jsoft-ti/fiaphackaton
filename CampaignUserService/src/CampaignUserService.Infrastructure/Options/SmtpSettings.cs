namespace CampaignUserService.Infrastructure.Options;

/// <summary>
/// Configuration for the future SMTP-backed <see cref="IEmailSender"/>
/// implementation. Bound from the "Smtp" section / environment variables.
/// </summary>
public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool EnableSsl { get; init; } = true;

    public string FromAddress { get; init; } = "no-reply@campanhas-sociais.org";

    public string FromName { get; init; } = "Campanhas Sociais";

    /// <summary>
    /// When false (default in this environment, since no SMTP credentials
    /// are configured), emails are only logged instead of actually sent.
    /// Set to true once real SMTP credentials are provided via environment
    /// variables to enable delivery.
    /// </summary>
    public bool Enabled { get; init; }

    public string PasswordResetUrlTemplate { get; init; } = "https://app.campanhas-sociais.org/reset-password?token={0}";
}
