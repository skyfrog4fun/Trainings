using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

/// <summary>
/// Manages trainer and participant feedback for a training. Feedback is only ever accepted
/// once the training has reached the Done state; see the training-lifecycle-redesign
/// documentation for the full set of eligibility and visibility rules.
/// </summary>
public interface IFeedbackService
{
    /// <summary>
    /// Builds the combined feedback view model for a training. When
    /// <paramref name="isStaffViewer"/> is true (AT/OT/GA/SA), the trainer feedback and the
    /// full (anonymized) list of participant feedback entries are included; otherwise only the
    /// caller's own participant feedback and the aggregate rating are included.
    /// </summary>
    Task<TrainingFeedbackOverviewDto> GetOverviewAsync(int trainingId, int requestingUserId, bool isStaffViewer, CancellationToken ct = default);

    /// <summary>Creates or updates the single trainer feedback entry for a training. AT-only, Done-only.</summary>
    Task UpsertTrainerFeedbackAsync(int trainerId, UpsertTrainerFeedbackDto dto, CancellationToken ct = default);

    /// <summary>Creates or updates the calling user's own participant feedback entry for a training. Registered-participant-only, Done-only.</summary>
    Task UpsertParticipantFeedbackAsync(int userId, UpsertParticipantFeedbackDto dto, CancellationToken ct = default);
}
