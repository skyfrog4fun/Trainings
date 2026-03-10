using Microsoft.EntityFrameworkCore;
using Trainings.Domain.Entities;
using Trainings.Domain.Interfaces;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly ApplicationDbContext _context;

    public AttendanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Attendance?> GetByIdAsync(int id) =>
        await _context.Attendances.FindAsync(id);

    public async Task<Attendance?> GetByUserAndTrainingAsync(int userId, int trainingId) =>
        await _context.Attendances
            .FirstOrDefaultAsync(a => a.UserId == userId && a.TrainingId == trainingId);

    public async Task<IEnumerable<Attendance>> GetByTrainingIdAsync(int trainingId) =>
        await _context.Attendances
            .Include(a => a.User)
            .Where(a => a.TrainingId == trainingId)
            .ToListAsync();

    public async Task<IEnumerable<Attendance>> GetByUserIdAsync(int userId) =>
        await _context.Attendances
            .Include(a => a.Training)
            .Where(a => a.UserId == userId)
            .ToListAsync();

    public async Task AddAsync(Attendance attendance)
    {
        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Attendance attendance)
    {
        _context.Attendances.Update(attendance);
        await _context.SaveChangesAsync();
    }

    public async Task UpsertAsync(Attendance attendance)
    {
        var existing = await GetByUserAndTrainingAsync(attendance.UserId, attendance.TrainingId);
        if (existing == null)
            await AddAsync(attendance);
        else
        {
            existing.Status = attendance.Status;
            existing.RecordedAt = attendance.RecordedAt;
            existing.RecordedByTrainerId = attendance.RecordedByTrainerId;
            await UpdateAsync(existing);
        }
    }
}
