using Trainings.Domain.Enums;

namespace Trainings.Domain.Entities;

public class GroupMembership
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
    public GroupMemberRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
