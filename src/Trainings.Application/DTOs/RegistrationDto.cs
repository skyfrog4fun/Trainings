using Trainings.Domain.Enums;

namespace Trainings.Application.DTOs;

public class RegistrationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TrainingId { get; set; }
    public string TrainingTitle { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public RegistrationStatus Status { get; set; }
}
