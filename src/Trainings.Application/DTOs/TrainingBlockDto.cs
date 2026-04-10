namespace Trainings.Application.DTOs;

public class TrainingBlockDto
{
    public int Id { get; set; }
    public int TrainingId { get; set; }
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public int? EffectiveDurationMinutes { get; set; }
    public string? TrainerComment { get; set; }
    public int? SourceBlockId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TagDto> Tags { get; set; } = new();
}

public class CreateTrainingBlockDto
{
    public int TrainingId { get; set; }
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public List<int> TagIds { get; set; } = new();
}

public class UpdateTrainingBlockDto
{
    public int Id { get; set; }
    public int OrderIndex { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public int? EffectiveDurationMinutes { get; set; }
    public string? TrainerComment { get; set; }
    public List<int> TagIds { get; set; } = new();
}

public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? GroupId { get; set; }
}

public class CreateTagDto
{
    public string Name { get; set; } = string.Empty;
    public int? GroupId { get; set; }
}
