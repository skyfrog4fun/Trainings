namespace Trainings.Domain.Entities;

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public bool IsSystemWide { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CountryId { get; set; }
    public Country? Country { get; set; }

    public ICollection<Group> DefaultForGroups { get; set; } = [];
    public ICollection<Training> Trainings { get; set; } = [];
    public ICollection<GroupLocation> AllowedForGroups { get; set; } = [];
}
