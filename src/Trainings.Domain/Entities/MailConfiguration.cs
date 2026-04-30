namespace Trainings.Domain.Entities;

public class MailConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public int Priority { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public int FailureCount { get; set; }
    public DateTime? LastFailedOn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMailConfiguration> GroupMailConfigurations { get; set; } = new List<GroupMailConfiguration>();
    public ICollection<NotificationLog> NotificationLogs { get; set; } = new List<NotificationLog>();
}
