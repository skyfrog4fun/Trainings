using Trainings.Domain.Enums;

namespace Trainings.Application.DTOs;

public class TrainingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? SpecialLocationDescription { get; set; }
    public string? MeetingPoint { get; set; }
    public DateTime DateTime { get; set; }
    public int? DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public TrainingStatus Status { get; set; }
    public int TrainerId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public int RegisteredCount { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? GroupCountry { get; set; }
    public bool AttendanceLocked { get; set; }
    public DateTime? AttendanceLockedAt { get; set; }
    public List<TrainingBlockDto> Blocks { get; set; } = [];
}

public class CreateTrainingDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? SpecialLocationDescription { get; set; }
    public string? MeetingPoint { get; set; }
    public DateTime DateTime { get; set; }
    public int? DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int TrainerId { get; set; }
    public int? GroupId { get; set; }
}

public class UpdateTrainingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? SpecialLocationDescription { get; set; }
    public string? MeetingPoint { get; set; }
    public DateTime DateTime { get; set; }
    public int? DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public TrainingStatus Status { get; set; }
    public int TrainerId { get; set; }
    public int? GroupId { get; set; }
}
