using Trainings.Domain.Enums;

namespace Trainings.Domain.Entities;

public class NotificationLog
{
    public int Id { get; set; }
    public NotificationAction Action { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public User? User { get; set; }
    public int? MailConfigurationId { get; set; }
    public MailConfiguration? MailConfiguration { get; set; }
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
