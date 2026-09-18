namespace Trainings.Domain.Entities;

public class Game
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsApproved { get; set; } = true;
    public bool IsSystemFallback { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TrainingBlockDefinition> TrainingBlockDefinitions { get; set; } = [];
}
