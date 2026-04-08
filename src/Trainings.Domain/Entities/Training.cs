namespace Trainings.Domain.Entities;

public class Training
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public int TrainerId { get; set; }
    public User Trainer { get; set; } = null!;
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<TrainingBlock> Blocks { get; set; } = new List<TrainingBlock>();
}
