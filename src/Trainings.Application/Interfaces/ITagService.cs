using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface ITagService
{
    Task<IEnumerable<TagDto>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TagDto>> GetByGroupAsync(int? groupId, CancellationToken ct = default);
    Task<TagDto> CreateAsync(CreateTagDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
