using Trainings.Domain.Enums;

namespace Trainings.Application.DTOs;

public class RegisterRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateOnly? Birthday { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? City { get; set; }
    public List<int> RequestedGroupIds { get; set; } = new();
    public string? WelcomeMessage { get; set; }
}
