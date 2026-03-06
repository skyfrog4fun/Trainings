using Trainings.Domain.Entities;

namespace Trainings.Domain.Interfaces;

public interface IAttendanceRepository
{
    Task<Attendance?> GetByIdAsync(int id);
    Task<Attendance?> GetByUserAndTrainingAsync(int userId, int trainingId);
    Task<IEnumerable<Attendance>> GetByTrainingIdAsync(int trainingId);
    Task<IEnumerable<Attendance>> GetByUserIdAsync(int userId);
    Task AddAsync(Attendance attendance);
    Task UpdateAsync(Attendance attendance);
    Task UpsertAsync(Attendance attendance);
}
