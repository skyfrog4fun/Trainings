using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface IStatisticsService
{
    Task<StatisticsDto> GetStatisticsAsync(bool isSuperAdmin, IReadOnlyCollection<int> managedGroupIds, CancellationToken ct = default);
}
