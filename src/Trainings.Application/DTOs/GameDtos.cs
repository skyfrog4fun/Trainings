namespace Trainings.Application.DTOs;

public class GameDto
{
    public int Id { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string EnglishText { get; set; } = string.Empty;
    public string GermanText { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public bool IsSystemFallback { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GameAdminDto
{
    public int Id { get; set; }
    public string EnglishText { get; set; } = string.Empty;
    public string GermanText { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public bool IsSystemFallback { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
