namespace Trainings.Application.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);
    Task SendEmailConfirmationAsync(string toEmail, string confirmLink, CancellationToken ct = default);
    Task SendAdminNewParticipantNotificationAsync(string adminEmail, string userName, CancellationToken ct = default);
    Task SendTestEmailAsync(string toEmail, CancellationToken ct = default);
    Task SendWelcomeWithPasswordResetAsync(string toEmail, string resetLink, CancellationToken ct = default);
}
