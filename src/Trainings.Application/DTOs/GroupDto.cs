using Trainings.Domain.Enums;

namespace Trainings.Application.DTOs;

public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
}

public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Identifier { get; set; }
    public string? Description { get; set; }
}

public class UpdateGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class GroupMembershipDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public GroupMemberRole Role { get; set; }
    public GroupMembershipStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
}

public class AddGroupMemberDto
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public GroupMemberRole Role { get; set; }
}
