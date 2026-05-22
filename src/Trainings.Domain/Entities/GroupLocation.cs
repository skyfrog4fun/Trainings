namespace Trainings.Domain.Entities;

public class GroupLocation
{
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
}
