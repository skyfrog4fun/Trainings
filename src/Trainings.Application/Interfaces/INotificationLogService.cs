using Trainings.Domain.Entities;
using Trainings.Domain.Enums;

namespace Trainings.Application.Interfaces;

public interface INotificationLogService
{
    Task LogAsync(NotificationAction action, string recipientEmail, int? userId, int? mailConfigurationId, int? groupId, bool isSuccess, string? errorMessage = null, Guid attemptId = default, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationLog>> GetRecentLogsAsync(int count = 50, CancellationToken ct = default);
    Task<int> GetSuccessCountAsync(DateTime since, CancellationToken ct = default);
    Task<int> GetFailureCountAsync(DateTime since, CancellationToken ct = default);
    Task<int> GetTotalSuccessCountAsync(CancellationToken ct = default);
    Task<int> GetTotalFailureCountAsync(CancellationToken ct = default);
}
