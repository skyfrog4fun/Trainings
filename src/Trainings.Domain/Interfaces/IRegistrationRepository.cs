using Trainings.Domain.Entities;

namespace Trainings.Domain.Interfaces;

public interface IRegistrationRepository
{
    Task<Registration?> GetByIdAsync(int id);
    Task<Registration?> GetByUserAndTrainingAsync(int userId, int trainingId);
    Task<IEnumerable<Registration>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Registration>> GetByTrainingIdAsync(int trainingId);
    Task AddAsync(Registration registration);
    Task UpdateAsync(Registration registration);
    Task DeleteAsync(int id);
}
