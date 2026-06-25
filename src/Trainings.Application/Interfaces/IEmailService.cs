using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface IEmailService
{
    Task<EmailSendResult> SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task<EmailSendResult> SendEmailConfirmationAsync(string toEmail, string confirmLink, CancellationToken ct = default);
    Task<EmailSendResult> SendAdminNewParticipantNotificationAsync(string adminEmail, string userName, string userEmail, string requestedGroups, string userDetailsLink, CancellationToken ct = default);
    Task<EmailSendResult> SendRegistrationApprovedAsync(string toEmail, string appLink, CancellationToken ct = default);
    Task<EmailSendResult> SendRegistrationRejectedAsync(string toEmail, string appLink, CancellationToken ct = default);
    Task<EmailSendResult> SendTrainingCancellationAsync(string toEmail, string trainingTitle, DateTime trainingDateTime, string appLink, CancellationToken ct = default);
    /// <summary>
    /// Sends a test email and returns the ordered per-configuration attempts.
    /// When mailConfigurationId is null, all configurations are tested in priority order.
    /// When mailConfigurationId is provided, that configuration is used even if inactive.
    /// </summary>
    Task<EmailSendResult> SendTestEmailAsync(string toEmail, int? mailConfigurationId = null, CancellationToken ct = default);
}
