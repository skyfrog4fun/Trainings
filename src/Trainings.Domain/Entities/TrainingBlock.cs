namespace Trainings.Domain.Entities;

public class TrainingBlock
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
    public int OrderIndex { get; set; }
    public int DefinitionId { get; set; }
    public TrainingBlockDefinition Definition { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public int MinParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
