namespace Trainings.Application.Interfaces;

public interface IPasswordResetService
{
    Task<EmailSendResult?> RequestResetAsync(string email, CancellationToken ct = default);
    Task ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);
}
