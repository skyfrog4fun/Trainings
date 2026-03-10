using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface IRegistrationService
{
    Task<IEnumerable<RegistrationDto>> GetByUserIdAsync(int userId);
    Task<IEnumerable<RegistrationDto>> GetByTrainingIdAsync(int trainingId);
    Task<RegistrationDto> RegisterAsync(int userId, int trainingId);
    Task CancelAsync(int userId, int trainingId);
    Task<bool> IsRegisteredAsync(int userId, int trainingId);
}
