namespace Trainings.Domain.Entities;

public class PendingGroupRequest
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
