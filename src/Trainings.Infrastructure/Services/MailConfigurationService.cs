using Microsoft.EntityFrameworkCore;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class MailConfigurationService : IMailConfigurationService
{
    private readonly ApplicationDbContext _context;

    public MailConfigurationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MailConfiguration>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.MailConfigurations
            .OrderBy(mc => mc.Priority)
            .ToListAsync(ct);
    }

    public async Task<MailConfiguration?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.MailConfigurations.FindAsync([id], ct);
    }

    public async Task<MailConfiguration> CreateAsync(MailConfiguration config, CancellationToken ct = default)
    {
        _context.MailConfigurations.Add(config);
        await _context.SaveChangesAsync(ct);
        return config;
    }

    public async Task UpdateAsync(MailConfiguration config, CancellationToken ct = default)
    {
        _context.MailConfigurations.Update(config);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var config = await _context.MailConfigurations.FindAsync([id], ct);
        if (config is not null)
        {
            _context.MailConfigurations.Remove(config);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<MailConfiguration>> GetActiveConfigsForGroupAsync(int? groupId, CancellationToken ct = default)
    {
        if (groupId.HasValue)
        {
            var groupConfigs = await _context.GroupMailConfigurations
                .Where(gmc => gmc.GroupId == groupId.Value)
                .Include(gmc => gmc.MailConfiguration)
                .OrderBy(gmc => gmc.Priority)
                .Select(gmc => gmc.MailConfiguration)
                .Where(mc => mc.IsActive)
                .ToListAsync(ct);

            if (groupConfigs.Count > 0)
            {
                return groupConfigs;
            }
        }

        return await _context.MailConfigurations
            .Where(mc => mc.IsActive)
            .OrderBy(mc => mc.Priority)
            .ToListAsync(ct);
    }

    public async Task ResetFailureCounterAsync(int id, CancellationToken ct = default)
    {
        var config = await _context.MailConfigurations.FindAsync([id], ct);
        if (config is not null)
        {
            config.FailureCount = 0;
            config.LastFailedOn = null;
            await _context.SaveChangesAsync(ct);
        }
    }
}
