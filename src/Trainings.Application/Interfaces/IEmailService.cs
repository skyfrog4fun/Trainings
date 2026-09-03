using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface IEmailService
{
    Task<EmailSendResult> SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task<EmailSendResult> SendEmailConfirmationAsync(string toEmail, string confirmLink, CancellationToken ct = default);
    /// <summary>
    /// Sends one "new participant" notification for a specific requested group: TO the group's admins, CC all SuperAdmins.
    /// </summary>
    Task<EmailSendResult> SendGroupAdminNewParticipantNotificationAsync(IReadOnlyCollection<string> groupAdminEmails, IReadOnlyCollection<string> superAdminEmails, string userName, string userEmail, int groupId, string groupName, string userDetailsLink, CancellationToken ct = default);
    /// <summary>
    /// Sends the "new participant" notification to SuperAdmins only. Used when the user did not request any group membership.
    /// </summary>
    Task<EmailSendResult> SendSuperAdminNewParticipantNotificationAsync(IReadOnlyCollection<string> superAdminEmails, string userName, string userEmail, string userDetailsLink, CancellationToken ct = default);
    Task<EmailSendResult> SendRegistrationApprovedAsync(string toEmail, string appLink, CancellationToken ct = default);
    Task<EmailSendResult> SendRegistrationRejectedAsync(string toEmail, string appLink, CancellationToken ct = default);
    Task<EmailSendResult> SendGroupMembershipApprovedAsync(string toEmail, int userId, int groupId, string groupName, string appLink, CancellationToken ct = default);
    Task<EmailSendResult> SendGroupMembershipDeclinedAsync(string toEmail, int userId, int groupId, string groupName, string appLink, CancellationToken ct = default);
    Task<EmailSendResult> SendTrainingCancellationAsync(string toEmail, string trainingTitle, DateTime trainingDateTime, string appLink, CancellationToken ct = default);
    /// <summary>
    /// Sends a test email and returns the ordered per-configuration attempts.
    /// When mailConfigurationId is null, all configurations are tested in priority order.
    /// When mailConfigurationId is provided, that configuration is used even if inactive.
    /// </summary>
    Task<EmailSendResult> SendTestEmailAsync(string toEmail, int? mailConfigurationId = null, CancellationToken ct = default);
}
