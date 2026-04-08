using Trainings.Domain.Enums;

namespace Trainings.Application.DTOs;

public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
}

public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
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
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class AddGroupMemberDto
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public GroupMemberRole Role { get; set; }
}
