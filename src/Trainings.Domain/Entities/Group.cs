namespace Trainings.Domain.Entities;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DayOfWeek? Weekday { get; set; }
    public int? LocationId { get; set; }
    public Location? Location { get; set; }
    public TimeOnly? StartTime { get; set; }
    public int? DurationMinutes { get; set; }
    public int? MaxParticipants { get; set; }
    public int? CountryId { get; set; }
    public Country? Country { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMembership> Memberships { get; set; } = new List<GroupMembership>();
    public ICollection<Training> Trainings { get; set; } = new List<Training>();
    public ICollection<GroupMailConfiguration> MailConfigurations { get; set; } = new List<GroupMailConfiguration>();
    public ICollection<GroupLocation> AllowedLocations { get; set; } = new List<GroupLocation>();
}
