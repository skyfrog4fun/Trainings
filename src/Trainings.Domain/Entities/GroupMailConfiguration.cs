namespace Trainings.Domain.Entities;

public class GroupMailConfiguration
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
    public int MailConfigurationId { get; set; }
    public MailConfiguration MailConfiguration { get; set; } = null!;
    public int Priority { get; set; } = 1;
}
