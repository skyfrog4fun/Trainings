using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task SendEmailConfirmationAsync(string toEmail, string confirmLink, CancellationToken ct = default);
    Task SendAdminNewParticipantNotificationAsync(string adminEmail, string userName, CancellationToken ct = default);
    /// <summary>
    /// Sends a test email and returns the ordered per-configuration attempts.
    /// When mailConfigurationId is null, all configurations are tested in priority order.
    /// When mailConfigurationId is provided, that configuration is used even if inactive.
    /// </summary>
    Task<EmailSendResult> SendTestEmailAsync(string toEmail, int? mailConfigurationId = null, CancellationToken ct = default);
    Task SendWelcomeWithPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);
}
