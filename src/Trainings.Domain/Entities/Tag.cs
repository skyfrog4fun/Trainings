namespace Trainings.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public ICollection<TrainingBlockTag> TrainingBlockTags { get; set; } = new List<TrainingBlockTag>();
}
