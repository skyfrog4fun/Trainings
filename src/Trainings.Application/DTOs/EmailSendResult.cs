namespace Trainings.Application.DTOs;

public sealed class EmailSendResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<EmailSendAttemptResult> Attempts { get; init; } = [];
}
