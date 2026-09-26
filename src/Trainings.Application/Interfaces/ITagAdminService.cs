using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface ITagAdminService
{
    Task<IReadOnlyList<TagAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TagDto>> GetActiveForSelectionAsync(CancellationToken ct = default);
    Task<TagAdminDto> CreateAsync(TagAdminDto dto, CancellationToken ct = default);
    Task UpdateAsync(TagAdminDto dto, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task ReactivateAsync(int id, CancellationToken ct = default);
}
