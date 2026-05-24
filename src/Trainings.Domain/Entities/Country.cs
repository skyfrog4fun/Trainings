namespace Trainings.Domain.Entities;

public class Country
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsRealCountry { get; set; } = true;

    public ICollection<Location> Locations { get; set; } = new List<Location>();
    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
