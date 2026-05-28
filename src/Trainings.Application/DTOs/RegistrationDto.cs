using Trainings.Domain.Enums;

namespace Trainings.Application.DTOs;

public class RegistrationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TrainingId { get; set; }
    public string TrainingTitle { get; set; } = string.Empty;
    public DateTime TrainingDateTime { get; set; }
    public string? TrainingGroupName { get; set; }
    public string? TrainingTrainerName { get; set; }
    public string? TrainingLocationName { get; set; }
    public int? TrainingDurationMinutes { get; set; }
    public string TrainingDescription { get; set; } = string.Empty;
    public int TrainingCapacity { get; set; }
    public int TrainingRegisteredCount { get; set; }
    public DateTime RegisteredAt { get; set; }
    public RegistrationStatus Status { get; set; }
}
