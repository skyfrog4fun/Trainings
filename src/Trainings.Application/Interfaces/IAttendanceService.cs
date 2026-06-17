using Trainings.Application.DTOs;
using Trainings.Domain.Enums;

namespace Trainings.Application.Interfaces;

public interface IAttendanceService
{
    Task<IEnumerable<AttendanceDto>> GetByTrainingIdAsync(int trainingId);
    Task<IEnumerable<AttendanceDto>> GetByUserIdAsync(int userId);
    Task RecordAttendanceAsync(int userId, int trainingId, AttendanceStatus status, int recordedByTrainerId);

    /// <summary>
    /// Persists attendance for all participants in the map in a single batch.
    /// </summary>
    Task BulkSaveAsync(int trainingId, IReadOnlyDictionary<int, AttendanceStatus> attendanceMap, int savedByTrainerId, CancellationToken ct = default);
}
