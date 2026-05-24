using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class StatisticsService : IStatisticsService
{
    private readonly ApplicationDbContext _context;

    public StatisticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StatisticsDto> GetStatisticsAsync(bool isSuperAdmin, IReadOnlyCollection<int> managedGroupIds, CancellationToken ct = default)
    {
        var scopedGroupIds = isSuperAdmin
            ? await _context.Groups.Select(g => g.Id).ToListAsync(ct)
            : managedGroupIds.Distinct().ToList();

        if (scopedGroupIds.Count == 0)
        {
            return new StatisticsDto();
        }

        var currentYear = DateTime.UtcNow.Year;

        var approvedMembershipsInScope = await _context.GroupMemberships
            .Where(gm =>
                scopedGroupIds.Contains(gm.GroupId) &&
                gm.Status == GroupMembershipStatus.Approved &&
                gm.IsActive)
            .Select(gm => new { gm.UserId, gm.GroupId })
            .ToListAsync(ct);

        var scopedUserIds = approvedMembershipsInScope
            .Select(m => m.UserId)
            .Distinct()
            .ToList();

        int activeUsers;
        int usersNotInGroup;

        if (isSuperAdmin)
        {
            activeUsers = await _context.Users.CountAsync(u => u.IsActive, ct);

            var usersWithActiveMembership = await _context.GroupMemberships
                .Where(gm => gm.Status == GroupMembershipStatus.Approved && gm.IsActive)
                .Select(gm => gm.UserId)
                .Distinct()
                .ToListAsync(ct);

            usersNotInGroup = await _context.Users
                .CountAsync(u => u.IsActive && !usersWithActiveMembership.Contains(u.Id), ct);
        }
        else
        {
            activeUsers = await _context.Users
                .CountAsync(u => u.IsActive && scopedUserIds.Contains(u.Id), ct);

            usersNotInGroup = await _context.Users
                .CountAsync(u =>
                    u.IsActive &&
                    scopedUserIds.Contains(u.Id) &&
                    !_context.GroupMemberships.Any(gm =>
                        gm.UserId == u.Id &&
                        gm.Status == GroupMembershipStatus.Approved &&
                        gm.IsActive &&
                        scopedGroupIds.Contains(gm.GroupId)), ct);
        }

        var trainingsInScope = await _context.Trainings
            .Where(t => t.GroupId.HasValue && scopedGroupIds.Contains(t.GroupId.Value))
            .Select(t => new { t.Id, t.DateTime })
            .ToListAsync(ct);

        var trainingIdsInScope = trainingsInScope.Select(t => t.Id).ToList();

        var totalRegistrations = await _context.Registrations
            .CountAsync(r => trainingIdsInScope.Contains(r.TrainingId), ct);

        var trainingIdsThisYear = trainingsInScope
            .Where(t => t.DateTime.Year == currentYear)
            .Select(t => t.Id)
            .ToList();

        var registeredCountThisYear = await _context.Registrations
            .CountAsync(r =>
                trainingIdsThisYear.Contains(r.TrainingId) &&
                r.Status == RegistrationStatus.Registered, ct);

        var avgUsersPerGroup = scopedGroupIds.Count == 0
            ? 0m
            : Math.Round((decimal)approvedMembershipsInScope.Count / scopedGroupIds.Count, 2);

        var avgParticipantsPerTraining = trainingIdsThisYear.Count == 0
            ? 0m
            : Math.Round((decimal)registeredCountThisYear / trainingIdsThisYear.Count, 2);

        return new StatisticsDto
        {
            TotalGroups = scopedGroupIds.Count,
            ActiveUsers = activeUsers,
            UsersNotInGroup = usersNotInGroup,
            TotalTrainings = trainingIdsInScope.Count,
            TotalRegistrations = totalRegistrations,
            AverageUsersPerGroup = avgUsersPerGroup,
            AverageParticipantsPerTraining = avgParticipantsPerTraining
        };
    }
}
