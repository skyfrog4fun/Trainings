using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface IUserRegistrationService
{
    Task<UserDto> RegisterAsync(RegisterRequestDto dto, CancellationToken ct = default);
    Task ConfirmEmailAsync(string token, CancellationToken ct = default);
    Task ApproveUserAsync(int userId, int adminUserId, CancellationToken ct = default);
    Task RejectUserAsync(int userId, int adminUserId, CancellationToken ct = default);
    Task<IEnumerable<UserDto>> GetPendingApprovalsAsync(CancellationToken ct = default);
}
