using Trainings.Domain.Enums;

namespace Trainings.Domain.Entities;

public class Attendance
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int TrainingId { get; set; }
    public Training Training { get; set; } = null!;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public int RecordedByTrainerId { get; set; }
}
