using Trainings.Application.DTOs;

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
}
