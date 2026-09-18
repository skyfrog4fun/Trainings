namespace Trainings.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string ColorToken { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<TrainingBlockDefinition> TrainingBlockDefinitions { get; set; } = [];
}
