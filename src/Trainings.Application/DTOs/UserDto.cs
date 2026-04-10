using Trainings.Domain.Enums;

namespace Trainings.Application.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public Gender Gender { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? Mobile { get; set; }
    public string? City { get; set; }
    public DateTime? EmailConfirmedAt { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? EntryDate { get; set; }
    public string? WelcomeMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Participant;
    public Gender Gender { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? Mobile { get; set; }
    public string? City { get; set; }
}

public class UpdateUserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public Gender Gender { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? Mobile { get; set; }
    public string? City { get; set; }
    public DateTime? EntryDate { get; set; }
    public string? WelcomeMessage { get; set; }
}
