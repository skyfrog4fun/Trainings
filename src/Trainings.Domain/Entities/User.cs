using Trainings.Domain.Enums;

namespace Trainings.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Participant;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Training> TrainingsAsTrainer { get; set; } = new List<Training>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
