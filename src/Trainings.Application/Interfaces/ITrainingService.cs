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

    // Block methods
    Task<IEnumerable<TrainingBlockDto>> GetBlocksAsync(int trainingId, CancellationToken ct = default);
    Task<TrainingBlockDto> AddBlockAsync(CreateTrainingBlockDto dto, CancellationToken ct = default);
    Task UpdateBlockAsync(UpdateTrainingBlockDto dto, CancellationToken ct = default);
    Task DeleteBlockAsync(int blockId, CancellationToken ct = default);
    Task CopyBlockAsync(int sourceBlockId, int targetTrainingId, CancellationToken ct = default);
    Task<IEnumerable<TrainingBlockDto>> GetAllBlocksLibraryAsync(CancellationToken ct = default);
}
