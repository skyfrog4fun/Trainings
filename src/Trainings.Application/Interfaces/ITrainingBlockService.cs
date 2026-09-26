using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface ITrainingBlockService
{
    Task<IReadOnlyList<TrainingBlockDto>> GetExecutionsAsync(int trainingId, CancellationToken ct = default);
    Task<TrainingBlockDto> CreateDefinitionAndAddExecutionAsync(CreateTrainingBlockDto dto, CancellationToken ct = default);
    Task<TrainingBlockDto> AddExecutionFromDefinitionAsync(AddTrainingBlockFromDefinitionDto dto, CancellationToken ct = default);
    Task UpdateExecutionAsync(UpdateTrainingBlockExecutionDto dto, CancellationToken ct = default);
    Task DeleteExecutionAsync(int executionId, CancellationToken ct = default);
    Task MoveExecutionUpAsync(int executionId, CancellationToken ct = default);
    Task MoveExecutionDownAsync(int executionId, CancellationToken ct = default);
}
