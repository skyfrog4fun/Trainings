using Microsoft.EntityFrameworkCore;
using Trainings.Domain.Entities;
using Trainings.Domain.Interfaces;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Repositories;

public class RegistrationRepository : IRegistrationRepository
{
    private readonly ApplicationDbContext _context;

    public RegistrationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Registration?> GetByIdAsync(int id) =>
        await _context.Registrations
            .Include(r => r.User)
            .Include(r => r.Training)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Registration?> GetByUserAndTrainingAsync(int userId, int trainingId) =>
        await _context.Registrations
            .Include(r => r.User)
            .Include(r => r.Training)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.TrainingId == trainingId);

    public async Task<IEnumerable<Registration>> GetByUserIdAsync(int userId) =>
        await _context.Registrations
            .Include(r => r.Training)
            .ThenInclude(t => t.Trainer)
            .Where(r => r.UserId == userId)
            .ToListAsync();

    public async Task<IEnumerable<Registration>> GetByTrainingIdAsync(int trainingId) =>
        await _context.Registrations
            .Include(r => r.User)
            .Where(r => r.TrainingId == trainingId)
            .ToListAsync();

    public async Task AddAsync(Registration registration)
    {
        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Registration registration)
    {
        _context.Registrations.Update(registration);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var reg = await _context.Registrations.FindAsync(id);
        if (reg != null)
        {
            _context.Registrations.Remove(reg);
            await _context.SaveChangesAsync();
        }
    }
}
