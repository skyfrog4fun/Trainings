using Trainings.Domain.Entities;

namespace Trainings.Domain.Interfaces;

public interface ITrainingRepository
{
    Task<Training?> GetByIdAsync(int id);
    Task<IEnumerable<Training>> GetAllAsync();
    Task<IEnumerable<Training>> GetActiveAsync();
    Task<IEnumerable<Training>> GetByTrainerIdAsync(int trainerId);
    Task AddAsync(Training training);
    Task UpdateAsync(Training training);
    Task DeleteAsync(int id);
}
