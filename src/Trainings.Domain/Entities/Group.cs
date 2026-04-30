namespace Trainings.Domain.Entities;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMembership> Memberships { get; set; } = new List<GroupMembership>();
    public ICollection<Training> Trainings { get; set; } = new List<Training>();
    public ICollection<GroupMailConfiguration> MailConfigurations { get; set; } = new List<GroupMailConfiguration>();
}
