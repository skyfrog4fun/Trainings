using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface ITrainingBlockLibraryService
{
    Task<TrainingBlockLibrarySearchResultDto> SearchAsync(TrainingBlockLibrarySearchDto dto, CancellationToken ct = default);
    Task<TrainingBlockDefinitionDto?> GetByIdAsync(int definitionId, CancellationToken ct = default);
}
