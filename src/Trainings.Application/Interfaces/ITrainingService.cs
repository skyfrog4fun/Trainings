using Trainings.Application.DTOs;

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

    /// <summary>
    /// Marks the attendance sheet for the specified training as locked, preventing participants
    /// from modifying their own registration status.
    /// </summary>
    Task LockAttendanceAsync(int trainingId, CancellationToken ct = default);
}
