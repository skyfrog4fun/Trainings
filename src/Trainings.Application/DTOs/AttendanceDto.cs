using Trainings.Domain.Enums;

namespace Trainings.Application.DTOs;

public class AttendanceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int TrainingId { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime RecordedAt { get; set; }
}
