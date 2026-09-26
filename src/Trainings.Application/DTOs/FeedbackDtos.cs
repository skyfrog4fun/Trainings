namespace Trainings.Application.DTOs;

/// <summary>Trainer feedback for a training, visible to AT/OT/GA/SA (never to U).</summary>
public class TrainerFeedbackDto
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public int TrainerId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public int? Rating { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class UpsertTrainerFeedbackDto
{
    public int TrainingId { get; set; }
    public string? Comment { get; set; }
    public int? Rating { get; set; }
}

/// <summary>
/// A single participant feedback entry, as seen by staff (AT/OT/GA/SA): content only, no
/// submitter identity (anonymized).
/// </summary>
public class ParticipantFeedbackEntryDto
{
    public string? Comment { get; set; }
    public int? Rating { get; set; }
    public DateTime SubmittedAt { get; set; }
}

/// <summary>A participant's own feedback entry, including their identity.</summary>
public class OwnParticipantFeedbackDto
{
    public int TrainingId { get; set; }
    public int UserId { get; set; }
    public string? Comment { get; set; }
    public int? Rating { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class UpsertParticipantFeedbackDto
{
    public int TrainingId { get; set; }
    public string? Comment { get; set; }
    public int? Rating { get; set; }
}

/// <summary>Aggregate participant rating, visible to everyone (U, AT, OT, GA, SA).</summary>
public class ParticipantFeedbackAggregateDto
{
    public double? AverageRating { get; set; }
    public int Count { get; set; }
}

/// <summary>Combined view model for the dedicated feedback page.</summary>
public class TrainingFeedbackOverviewDto
{
    public int TrainingId { get; set; }
    public TrainerFeedbackDto? TrainerFeedback { get; set; }
    public IReadOnlyList<ParticipantFeedbackEntryDto> ParticipantFeedbackEntries { get; set; } = [];
    public OwnParticipantFeedbackDto? OwnParticipantFeedback { get; set; }
    public ParticipantFeedbackAggregateDto ParticipantAggregate { get; set; } = new();
}
