namespace Trainings.Application.DTOs;

public class TrainingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public int TrainerId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public int RegisteredCount { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; }
    public List<TrainingBlockDto> Blocks { get; set; } = new();
}

public class CreateTrainingDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int Capacity { get; set; }
    public int TrainerId { get; set; }
    public int GroupId { get; set; }
}

public class UpdateTrainingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public int GroupId { get; set; }
}
