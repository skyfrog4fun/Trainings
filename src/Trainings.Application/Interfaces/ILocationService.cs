using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface ILocationService
{
    Task<IEnumerable<LocationDto>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<LocationDto>> GetByGroupIdAsync(int groupId, CancellationToken ct = default);
    Task<LocationDto> CreateAsync(CreateLocationDto dto, CancellationToken ct = default);
    Task UpdateAsync(UpdateLocationDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
