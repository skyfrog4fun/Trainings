namespace Trainings.Domain.Entities;

/// <summary>
/// Overall trainer feedback for a training. Exactly one row per training, submitted (and
/// resubmitted/overwritten) by the assigned trainer only, once the training is Done.
/// </summary>
public class TrainerFeedback
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
    public int TrainerId { get; set; }
    public User Trainer { get; set; } = null!;
    public string? Comment { get; set; }
    public int? Rating { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
