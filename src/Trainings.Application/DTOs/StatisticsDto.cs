namespace Trainings.Application.DTOs;

public class StatisticsDto
{
    public int TotalGroups { get; set; }
    public int ActiveUsers { get; set; }
    public int UsersNotInGroup { get; set; }
    public int TotalTrainings { get; set; }
    public int TotalRegistrations { get; set; }
    public decimal AverageUsersPerGroup { get; set; }
    public decimal AverageParticipantsPerTraining { get; set; }
}
