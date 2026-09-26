namespace Trainings.Domain.Entities;

/// <summary>
/// Participant feedback for a training. Exactly one row per participant per training,
/// submitted (and resubmitted/overwritten) by that participant only, once the training is Done.
/// Any registered participant may submit, regardless of attendance status.
/// </summary>
public class ParticipantFeedback
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Comment { get; set; }
    public int? Rating { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
