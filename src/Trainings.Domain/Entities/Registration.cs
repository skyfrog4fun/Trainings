using Trainings.Domain.Enums;

namespace Trainings.Domain.Entities;

public class Registration
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Registered;
}
