using Microsoft.EntityFrameworkCore;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class NotificationLogService(ApplicationDbContext context) : INotificationLogService
{
    private readonly ApplicationDbContext _context = context;

    public async Task LogAsync(NotificationAction action, string recipientEmail, int? userId, int? mailConfigurationId, int? groupId, bool isSuccess, string? errorMessage = null, Guid attemptId = default, CancellationToken ct = default)
    {
        var log = new NotificationLog
        {
            AttemptId = attemptId == default ? Guid.NewGuid() : attemptId,
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

    public async Task<IReadOnlyList<NotificationLog>> GetRecentLogsAsync(int count = 50, int? afterLogId = null, CancellationToken ct = default)
    {
        var query = _context.NotificationLogs.AsQueryable();
        if (afterLogId.HasValue)
        {
            query = query.Where(nl => nl.Id > afterLogId.Value);
        }

        return await query
            .OrderByDescending(nl => nl.CreatedAt)
            .Take(count)
            .Include(nl => nl.User)
            .Include(nl => nl.MailConfiguration)
            .ToListAsync(ct);
    }

    public async Task<int?> GetResetPointerLogIdAsync(CancellationToken ct = default)
    {
        var state = await _context.NotificationFeedStates
            .FirstOrDefaultAsync(x => x.Id == 1, ct);
        return state?.ResetPointerLogId;
    }

    public async Task SetResetPointerLogIdAsync(int? logId, CancellationToken ct = default)
    {
        var state = await _context.NotificationFeedStates
            .FirstOrDefaultAsync(x => x.Id == 1, ct);

        if (state is null)
        {
            state = new NotificationFeedState
            {
                Id = 1,
                ResetPointerLogId = logId,
                UpdatedAt = DateTime.UtcNow
            };
            _context.NotificationFeedStates.Add(state);
        }
        else
        {
            state.ResetPointerLogId = logId;
            state.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
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
