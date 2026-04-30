namespace Trainings.Domain.Entities;

public class SlugRedirect
{
    public int Id { get; set; }
    public string OldSlug { get; set; } = string.Empty;
    public string NewSlug { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
