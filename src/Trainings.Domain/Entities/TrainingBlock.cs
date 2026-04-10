namespace Trainings.Domain.Entities;

public class TrainingBlock
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public int? EffectiveDurationMinutes { get; set; }
    public string? TrainerComment { get; set; }
    public int? SourceBlockId { get; set; }
    public TrainingBlock? SourceBlock { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TrainingBlockTag> TrainingBlockTags { get; set; } = new List<TrainingBlockTag>();
}
