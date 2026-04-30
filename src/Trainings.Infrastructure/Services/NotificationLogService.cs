using Microsoft.EntityFrameworkCore;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class NotificationLogService : INotificationLogService
{
    private readonly ApplicationDbContext _context;

    public NotificationLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(NotificationAction action, string recipientEmail, int? userId, int? mailConfigurationId, int? groupId, bool isSuccess, string? errorMessage = null, CancellationToken ct = default)
    {
        var log = new NotificationLog
        {
            Action = action,
            RecipientEmail = recipientEmail,
            UserId = userId,
            MailConfigurationId = mailConfigurationId,
            GroupId = groupId,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        };

        _context.NotificationLogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationLog>> GetRecentLogsAsync(int count = 50, CancellationToken ct = default)
    {
        return await _context.NotificationLogs
            .OrderByDescending(nl => nl.CreatedAt)
            .Take(count)
            .Include(nl => nl.User)
            .Include(nl => nl.MailConfiguration)
            .ToListAsync(ct);
    }

    public async Task<int> GetSuccessCountAsync(DateTime since, CancellationToken ct = default)
    {
        return await _context.NotificationLogs
            .CountAsync(nl => nl.IsSuccess && nl.CreatedAt >= since, ct);
    }

    public async Task<int> GetFailureCountAsync(DateTime since, CancellationToken ct = default)
    {
        return await _context.NotificationLogs
            .CountAsync(nl => !nl.IsSuccess && nl.CreatedAt >= since, ct);
    }

    public async Task<int> GetTotalSuccessCountAsync(CancellationToken ct = default)
    {
        return await _context.NotificationLogs
            .CountAsync(nl => nl.IsSuccess, ct);
    }

    public async Task<int> GetTotalFailureCountAsync(CancellationToken ct = default)
    {
        return await _context.NotificationLogs
            .CountAsync(nl => !nl.IsSuccess, ct);
    }
}
