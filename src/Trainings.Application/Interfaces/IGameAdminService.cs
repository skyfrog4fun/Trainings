using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface IGameAdminService
{
    Task<IReadOnlyList<GameAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<GameDto>> GetActiveForSelectionAsync(CancellationToken ct = default);
    Task<GameDto> CreateAdHocAsync(string name, int requestingUserId, CancellationToken ct = default);
    Task ApproveAsync(int id, CancellationToken ct = default);
    Task RenameAsync(int id, string englishText, string germanText, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task ReactivateAsync(int id, CancellationToken ct = default);
}
