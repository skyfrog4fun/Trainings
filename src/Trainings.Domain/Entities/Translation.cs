using Trainings.Domain.Enums;

namespace Trainings.Domain.Entities;

public class Translation
{
    public int Id { get; set; }
    public TranslationEntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public string Culture { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
