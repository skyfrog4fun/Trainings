using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Domain.Interfaces;

namespace Trainings.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;

    public AttendanceService(IAttendanceRepository attendanceRepository)
    {
        _attendanceRepository = attendanceRepository;
    }

    public async Task<IEnumerable<AttendanceDto>> GetByTrainingIdAsync(int trainingId)
    {
        var attendances = await _attendanceRepository.GetByTrainingIdAsync(trainingId);
        return attendances.Select(MapToDto);
    }

    public async Task<IEnumerable<AttendanceDto>> GetByUserIdAsync(int userId)
    {
        var attendances = await _attendanceRepository.GetByUserIdAsync(userId);
        return attendances.Select(MapToDto);
    }

    public async Task RecordAttendanceAsync(int userId, int trainingId, AttendanceStatus status, int recordedByTrainerId)
    {
        var existing = await _attendanceRepository.GetByUserAndTrainingAsync(userId, trainingId);
        if (existing != null)
        {
            existing.Status = status;
            existing.RecordedAt = DateTime.UtcNow;
            existing.RecordedByTrainerId = recordedByTrainerId;
            await _attendanceRepository.UpdateAsync(existing);
        }
        else
        {
            var attendance = new Attendance
            {
                UserId = userId,
                TrainingId = trainingId,
                Status = status,
                RecordedAt = DateTime.UtcNow,
                RecordedByTrainerId = recordedByTrainerId
            };
            await _attendanceRepository.AddAsync(attendance);
        }
    }

    private static AttendanceDto MapToDto(Attendance a) => new()
    {
        Id = a.Id,
        UserId = a.UserId,
        UserName = a.User?.Name ?? string.Empty,
        TrainingId = a.TrainingId,
        Status = a.Status,
        RecordedAt = a.RecordedAt
    };
}
