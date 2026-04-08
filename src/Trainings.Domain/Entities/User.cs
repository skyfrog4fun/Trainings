using Trainings.Domain.Enums;

namespace Trainings.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Participant;
    public bool IsActive { get; set; } = true;
    public Gender Gender { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? Mobile { get; set; }
    public string? City { get; set; }
    public DateTime? EmailConfirmedAt { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public DateTime? EntryDate { get; set; }
    public string? WelcomeMessage { get; set; }

    /// <summary>Kept for backwards compatibility with EF migrations from CreatedAt.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    public ICollection<Training> TrainingsAsTrainer { get; set; } = new List<Training>();
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<EmailConfirmationToken> EmailConfirmationTokens { get; set; } = new List<EmailConfirmationToken>();
}
