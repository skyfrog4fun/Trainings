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

    public GroupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GroupDto>> GetAllAsync(CancellationToken ct = default)
    {
        var groups = await _context.Groups
            .Include(g => g.Memberships)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);
        return groups.Select(MapToDto);
    }

    public async Task<GroupDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var group = await _context.Groups
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.Id == id, ct);
        return group == null ? null : MapToDto(group);
    }

    public async Task<GroupDto> CreateAsync(CreateGroupDto dto, CancellationToken ct = default)
    {
        var group = new Group
        {
            Name = dto.Name,
            Slug = GenerateSlug(dto.Name),
            Identifier = dto.Identifier ?? GenerateSlug(dto.Name),
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync(ct);
        return MapToDto(group);
    }

    public async Task UpdateAsync(UpdateGroupDto dto, CancellationToken ct = default)
    {
        var group = await _context.Groups.FindAsync([dto.Id], ct)
            ?? throw new InvalidOperationException($"Group {dto.Id} not found.");

        var oldSlug = group.Slug;
        var newSlug = GenerateSlug(dto.Name);

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
        group.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
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
        var membership = await _context.GroupMemberships.FindAsync([membershipId], ct);
        if (membership != null)
        {
            _context.GroupMemberships.Remove(membership);
            await _context.SaveChangesAsync(ct);
        }
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
        Description = group.Description,
        IsActive = group.IsActive,
        CreatedAt = group.CreatedAt,
        MemberCount = group.Memberships.Count(m => m.Status == GroupMembershipStatus.Approved)
    };

    private static GroupMembershipDto MapMembershipToDto(GroupMembership gm) => new()
    {
        Id = gm.Id,
        UserId = gm.UserId,
        UserDisplayName = gm.User.DisplayName,
        UserEmail = gm.User.Email,
        GroupId = gm.GroupId,
        Role = gm.Role,
        IsActive = gm.IsActive,
        JoinedAt = gm.JoinedAt
    };
}
