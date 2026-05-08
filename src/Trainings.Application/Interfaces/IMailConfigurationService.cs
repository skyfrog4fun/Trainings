using Trainings.Domain.Entities;

namespace Trainings.Application.Interfaces;

public interface IMailConfigurationService
{
    Task<IReadOnlyList<MailConfiguration>> GetAllAsync(CancellationToken ct = default);
    Task<MailConfiguration?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<MailConfiguration> CreateAsync(MailConfiguration config, CancellationToken ct = default);
    Task UpdateAsync(MailConfiguration config, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<MailConfiguration>> GetActiveConfigsForGroupAsync(int? groupId, CancellationToken ct = default);
    Task RecordSuccessfulSendAsync(int id, DateTime sentAtUtc, CancellationToken ct = default);
    Task RecordFailedSendAsync(int id, string errorMessage, CancellationToken ct = default);
}
