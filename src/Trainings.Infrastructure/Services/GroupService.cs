using Microsoft.EntityFrameworkCore;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;

namespace Trainings.Infrastructure.Services;

public class GroupService : IGroupService
{
    private readonly ApplicationDbContext _context;
    private readonly IAppRuntimeModeService _appRuntimeModeService;

    public GroupService(ApplicationDbContext context, IAppRuntimeModeService appRuntimeModeService)
    {
        _context = context;
        _appRuntimeModeService = appRuntimeModeService;
    }

    public async Task<IEnumerable<GroupDto>> GetAllAsync(CancellationToken ct = default)
    {
        var groups = await _context.Groups
            .Include(g => g.Memberships)
            .Include(g => g.Location)
            .Include(g => g.AllowedLocations)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);
        return groups.Select(MapToDto);
    }

    public async Task<GroupDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var group = await _context.Groups
            .Include(g => g.Memberships)
            .Include(g => g.Location)
            .Include(g => g.AllowedLocations)
            .FirstOrDefaultAsync(g => g.Id == id, ct);
        return group == null ? null : MapToDto(group);
    }

    public async Task<GroupDto?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var group = await _context.Groups
            .Include(g => g.Memberships)
            .Include(g => g.Location)
            .Include(g => g.AllowedLocations)
            .FirstOrDefaultAsync(g => g.Slug == slug, ct);
        return group == null ? null : MapToDto(group);
    }

    public async Task<GroupDto> CreateAsync(CreateGroupDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var slug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Name) : GenerateSlug(dto.Slug);
        var group = new Group
        {
            Name = dto.Name,
            Slug = slug,
            Identifier = dto.Identifier ?? slug,
            Description = dto.Description,
            Weekday = dto.Weekday,
            LocationId = dto.LocationId,
            StartTime = dto.StartTime,
            DurationMinutes = dto.DurationMinutes,
            MaxParticipants = dto.MaxParticipants,
            Country = string.IsNullOrWhiteSpace(dto.Country) ? "CH" : dto.Country.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync(ct);

        if (dto.AllowedLocationIds.Count > 0)
        {
            var assignments = dto.AllowedLocationIds
                .Distinct()
                .Select(locationId => new GroupLocation { GroupId = group.Id, LocationId = locationId });
            _context.GroupLocations.AddRange(assignments);
            await _context.SaveChangesAsync(ct);
        }

        await _context.Entry(group).Reference(g => g.Location).LoadAsync(ct);
        await _context.Entry(group).Collection(g => g.AllowedLocations).LoadAsync(ct);
        return MapToDto(group);
    }

    public async Task UpdateAsync(UpdateGroupDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var group = await _context.Groups
            .Include(g => g.AllowedLocations)
            .FirstOrDefaultAsync(g => g.Id == dto.Id, ct)
            ?? throw new InvalidOperationException($"Group {dto.Id} not found.");

        var oldSlug = group.Slug;
        var newSlug = string.IsNullOrWhiteSpace(dto.Slug) ? GenerateSlug(dto.Name) : GenerateSlug(dto.Slug);

        if (!string.Equals(oldSlug, newSlug, StringComparison.Ordinal) && !string.IsNullOrEmpty(oldSlug))
        {
            _context.SlugRedirects.Add(new SlugRedirect
            {
                OldSlug = oldSlug,
                NewSlug = newSlug,
                EntityType = "Group",
                ChangedAt = DateTime.UtcNow
            });
        }

        group.Name = dto.Name;
        group.Slug = newSlug;
        group.Description = dto.Description;
        group.Weekday = dto.Weekday;
        group.LocationId = dto.LocationId;
        group.StartTime = dto.StartTime;
        group.DurationMinutes = dto.DurationMinutes;
        group.MaxParticipants = dto.MaxParticipants;
        group.Country = string.IsNullOrWhiteSpace(dto.Country) ? "CH" : dto.Country.Trim().ToUpperInvariant();
        group.IsActive = dto.IsActive;

        var requestedLocationIds = dto.AllowedLocationIds.Distinct().ToHashSet();
        var existingLocationIds = group.AllowedLocations.Select(x => x.LocationId).ToHashSet();

        var toRemove = group.AllowedLocations.Where(x => !requestedLocationIds.Contains(x.LocationId)).ToList();
        if (toRemove.Count > 0)
        {
            _context.GroupLocations.RemoveRange(toRemove);
        }

        var toAdd = requestedLocationIds.Where(id => !existingLocationIds.Contains(id))
            .Select(id => new GroupLocation { GroupId = group.Id, LocationId = id });
        _context.GroupLocations.AddRange(toAdd);

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var group = await _context.Groups.FindAsync([id], ct);
        if (group != null)
        {
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IEnumerable<GroupMembershipDto>> GetMembersAsync(int groupId, CancellationToken ct = default)
    {
        var memberships = await _context.GroupMemberships
            .Include(gm => gm.User)
            .Where(gm => gm.GroupId == groupId)
            .ToListAsync(ct);
        return memberships.Select(MapMembershipToDto);
    }

    public async Task AddMemberAsync(AddGroupMemberDto dto, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var membership = new GroupMembership
        {
            UserId = dto.UserId,
            GroupId = dto.GroupId,
            Role = dto.Role,
            Status = GroupMembershipStatus.Approved,
            IsActive = true,
            RequestedAt = DateTime.UtcNow,
            ApprovedAt = DateTime.UtcNow,
            JoinedAt = DateTime.UtcNow
        };
        _context.GroupMemberships.Add(membership);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(int membershipId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var membership = await _context.GroupMemberships.FindAsync([membershipId], ct);
        if (membership != null)
        {
            _context.GroupMemberships.Remove(membership);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<IEnumerable<GroupMembershipDto>> GetAllMembershipsForUserAsync(int userId, CancellationToken ct = default)
    {
        var memberships = await _context.GroupMemberships
            .Include(gm => gm.User)
            .Include(gm => gm.Group)
            .Where(gm => gm.UserId == userId)
            .OrderBy(gm => gm.Group.Name)
            .ToListAsync(ct);
        return memberships.Select(MapMembershipToDto);
    }

    public async Task ApproveMemberAsync(int membershipId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var membership = await _context.GroupMemberships.FindAsync([membershipId], ct)
            ?? throw new InvalidOperationException($"Membership {membershipId} not found.");
        membership.Status = GroupMembershipStatus.Approved;
        membership.ApprovedAt = DateTime.UtcNow;
        membership.IsActive = true;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeclineMemberAsync(int membershipId, CancellationToken ct = default)
    {
        _appRuntimeModeService.EnsureWriteAllowed();

        var membership = await _context.GroupMemberships.FindAsync([membershipId], ct)
            ?? throw new InvalidOperationException($"Membership {membershipId} not found.");
        membership.Status = GroupMembershipStatus.Declined;
        membership.DeclinedAt = DateTime.UtcNow;
        membership.IsActive = false;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<GroupDto>> GetGroupsForUserAsync(int userId, CancellationToken ct = default)
    {
        var groups = await _context.GroupMemberships
            .Include(gm => gm.Group)
                .ThenInclude(g => g.Memberships)
            .Where(gm => gm.UserId == userId && gm.IsActive && gm.Status == GroupMembershipStatus.Approved)
            .Select(gm => gm.Group)
            .Distinct()
            .ToListAsync(ct);
        return groups.Select(MapToDto);
    }

    public async Task<IEnumerable<GroupMembershipDto>> GetApprovedMembershipsForUserAsync(int userId, CancellationToken ct = default)
    {
        var memberships = await _context.GroupMemberships
            .Include(gm => gm.User)
            .Where(gm => gm.UserId == userId && gm.Status == GroupMembershipStatus.Approved && gm.IsActive)
            .ToListAsync(ct);
        return memberships.Select(MapMembershipToDto);
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace("ä", "ae")
            .Replace("ö", "oe")
            .Replace("ü", "ue")
            .Replace("ß", "ss");

        // Replace any non-alphanumeric characters with hyphens
        var builder = new System.Text.StringBuilder(slug.Length);
        foreach (var c in slug)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
            }
            else
            {
                builder.Append('-');
            }
        }

        // Collapse multiple hyphens and trim
        slug = builder.ToString();
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return slug.Trim('-');
    }

    private static GroupDto MapToDto(Group group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Slug = group.Slug,
        Identifier = group.Identifier,
        Description = group.Description,
        Weekday = group.Weekday,
        LocationId = group.LocationId,
        LocationName = group.Location?.Name,
        StartTime = group.StartTime,
        DurationMinutes = group.DurationMinutes,
        MaxParticipants = group.MaxParticipants,
        Country = group.Country,
        AllowedLocationIds = group.AllowedLocations.Select(x => x.LocationId).ToList(),
        IsActive = group.IsActive,
        CreatedAt = group.CreatedAt,
        MemberCount = group.Memberships.Count(m => m.Status == GroupMembershipStatus.Approved)
    };

    private static GroupMembershipDto MapMembershipToDto(GroupMembership gm) => new()
    {
        Id = gm.Id,
        UserId = gm.UserId,
        UserDisplayName = gm.User?.DisplayName ?? string.Empty,
        UserEmail = gm.User?.Email ?? string.Empty,
        GroupId = gm.GroupId,
        Role = gm.Role,
        Status = gm.Status,
        IsActive = gm.IsActive,
        JoinedAt = gm.JoinedAt,
        RequestedAt = gm.RequestedAt,
        ApprovedAt = gm.ApprovedAt,
        DeclinedAt = gm.DeclinedAt
    };
}
