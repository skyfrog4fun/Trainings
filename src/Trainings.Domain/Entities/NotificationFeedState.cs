namespace Trainings.Domain.Entities;

public class NotificationFeedState
{
    public int Id { get; set; } = 1;
    public int? ResetPointerLogId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
