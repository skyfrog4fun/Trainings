namespace Trainings.Application.DTOs;

public sealed class EmailConfirmationResultDto
{
    public bool IsSuccess { get; init; }
    public bool IsExpired { get; init; }
    public int? UserId { get; init; }
    public string Message { get; init; } = string.Empty;
}
