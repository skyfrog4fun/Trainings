namespace Trainings.Domain.Entities;

public class TrainingBlockTag
{
    public int TrainingBlockId { get; set; }
    public TrainingBlock TrainingBlock { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
