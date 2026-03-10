using Microsoft.EntityFrameworkCore;
using Trainings.Domain.Entities;
using Trainings.Domain.Interfaces;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Repositories;

public class TrainingRepository : ITrainingRepository
{
    private readonly ApplicationDbContext _context;

    public TrainingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Training?> GetByIdAsync(int id) =>
        await _context.Trainings
            .Include(t => t.Trainer)
            .Include(t => t.Registrations)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<IEnumerable<Training>> GetAllAsync() =>
        await _context.Trainings
            .Include(t => t.Trainer)
            .Include(t => t.Registrations)
            .OrderByDescending(t => t.DateTime)
            .ToListAsync();

    public async Task<IEnumerable<Training>> GetActiveAsync() =>
        await _context.Trainings
            .Include(t => t.Trainer)
            .Include(t => t.Registrations)
            .Where(t => t.IsActive)
            .OrderBy(t => t.DateTime)
            .ToListAsync();

    public async Task<IEnumerable<Training>> GetByTrainerIdAsync(int trainerId) =>
        await _context.Trainings
            .Include(t => t.Trainer)
            .Include(t => t.Registrations)
            .Where(t => t.TrainerId == trainerId)
            .OrderByDescending(t => t.DateTime)
            .ToListAsync();

    public async Task AddAsync(Training training)
    {
        _context.Trainings.Add(training);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Training training)
    {
        _context.Trainings.Update(training);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var training = await _context.Trainings.FindAsync(id);
        if (training != null)
        {
            _context.Trainings.Remove(training);
            await _context.SaveChangesAsync();
        }
    }
}
