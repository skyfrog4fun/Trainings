using Trainings.Application.DTOs;
using Trainings.Domain.Enums;

namespace Trainings.Application.Interfaces;

public interface ITrainingService
{
    Task<TrainingDto?> GetByIdAsync(int id);
    Task<IEnumerable<TrainingDto>> GetAllAsync();
    Task<IEnumerable<TrainingDto>> GetActiveAsync();
    Task<IEnumerable<TrainingDto>> GetByTrainerIdAsync(int trainerId);
    Task<TrainingDto> CreateAsync(CreateTrainingDto dto);
    Task UpdateAsync(UpdateTrainingDto dto);
    Task DeleteAsync(int id);

    // Block methods
    Task<IEnumerable<TrainingBlockDto>> GetBlocksAsync(int trainingId, CancellationToken ct = default);
    Task<TrainingBlockDto> AddBlockAsync(CreateTrainingBlockDto dto, CancellationToken ct = default);
    Task UpdateBlockAsync(UpdateTrainingBlockDto dto, CancellationToken ct = default);
    Task DeleteBlockAsync(int blockId, CancellationToken ct = default);
    Task CopyBlockAsync(int sourceBlockId, int targetTrainingId, CancellationToken ct = default);
    Task<IEnumerable<TrainingBlockDto>> GetAllBlocksLibraryAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the next calendar date matching <paramref name="weekday"/> that has no
    /// existing training for <paramref name="groupId"/>.  Steps forward by 7 days when
    /// every occurrence is already occupied.
    /// </summary>
    Task<DateTime> GetNextAvailableDateForGroupAsync(int groupId, DayOfWeek weekday, CancellationToken ct = default);

    /// <summary>Explicitly sets the lifecycle status of the training.</summary>
    Task SetStatusAsync(int trainingId, TrainingStatus status, CancellationToken ct = default);

    /// <summary>
    /// Marks the attendance sheet for the specified training as locked, preventing participants
    /// from modifying their own registration status, and moves the training to <see cref="TrainingStatus.Done"/>.
    /// </summary>
    Task LockAttendanceAsync(int trainingId, CancellationToken ct = default);

    /// <summary>
    /// A Trainer self-assigns to an unassigned (<see cref="TrainingStatus.New"/>) training,
    /// moving it to <see cref="TrainingStatus.InPlanning"/>.
    /// </summary>
    Task<TrainingDto> TakeAsync(int trainingId, int trainerId, CancellationToken ct = default);

    /// <summary>
    /// The currently assigned Trainer releases (un-assigns) themself, moving the training
    /// back to <see cref="TrainingStatus.New"/>. Not allowed once the training is in progress or done.
    /// </summary>
    Task ReleaseTrainerAsync(int trainingId, int requestingUserId, CancellationToken ct = default);

    /// <summary>
    /// GroupAdmin/SuperAdmin action to assign, reassign, or clear (<paramref name="newTrainerId"/> = null)
    /// the trainer of a training. Not allowed once the training is in progress or done.
    /// </summary>
    Task ReassignTrainerAsync(int trainingId, int? newTrainerId, CancellationToken ct = default);

    /// <summary>
    /// The assigned Trainer (or GroupAdmin/SuperAdmin) confirms planning is finished,
    /// moving the training from <see cref="TrainingStatus.InPlanning"/> to <see cref="TrainingStatus.Planned"/>.
    /// </summary>
    Task ConfirmPlannedAsync(int trainingId, CancellationToken ct = default);

    /// <summary>
    /// The assigned Trainer manually starts the training, moving it from
    /// <see cref="TrainingStatus.Planned"/> to <see cref="TrainingStatus.InProgress"/> and closing registration.
    /// </summary>
    Task StartAsync(int trainingId, int requestingTrainerId, CancellationToken ct = default);
}
