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
    Task<DateTime> GetNextAvailableDateForGroupAsync(int groupId, DayOfWeek weekday, CancellationToken ct = default);
    Task SetStatusAsync(int trainingId, TrainingStatus status, CancellationToken ct = default);
    Task LockAttendanceAsync(int trainingId, CancellationToken ct = default);
    Task<TrainingDto> TakeAsync(int trainingId, int trainerId, CancellationToken ct = default);
    Task ReleaseTrainerAsync(int trainingId, int requestingUserId, CancellationToken ct = default);
    Task ReassignTrainerAsync(int trainingId, int? newTrainerId, CancellationToken ct = default);
    Task ConfirmPlannedAsync(int trainingId, CancellationToken ct = default);
    Task StartAsync(int trainingId, int requestingTrainerId, CancellationToken ct = default);
}
