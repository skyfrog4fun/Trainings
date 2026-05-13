namespace Trainings.Application.DTOs;

public sealed class RegistrationResultDto
{
    public required UserDto User { get; init; }
    public required EmailSendResult ConfirmationEmail { get; init; }
}
