namespace Trainings.Domain.Entities;

public class TrainingBlockDefinition
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
    public int? GameId { get; set; }
    public Game? Game { get; set; }
    public int MinParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    public bool IsGlobal { get; set; }
    public int CreatorId { get; set; }
    public User Creator { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<TrainingBlock> Executions { get; set; } = [];
}
