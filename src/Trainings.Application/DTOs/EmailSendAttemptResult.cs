namespace Trainings.Application.DTOs;

public sealed class EmailSendAttemptResult
{
    public required int MailConfigurationId { get; init; }
    public required string ConfigurationName { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsSuccess { get; init; }
    public required string Message { get; init; }
}
