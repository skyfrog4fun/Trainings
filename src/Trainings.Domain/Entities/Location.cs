namespace Trainings.Domain.Entities;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public bool IsSystemWide { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Group> DefaultForGroups { get; set; } = new List<Group>();
    public ICollection<Training> Trainings { get; set; } = new List<Training>();
    public ICollection<GroupLocation> AllowedForGroups { get; set; } = new List<GroupLocation>();
}
