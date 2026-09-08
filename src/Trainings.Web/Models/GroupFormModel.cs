using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;

namespace Trainings.Web.Models;

public class GroupFormModel : IValidatableObject
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DayOfWeek? Weekday { get; set; }
    public int? LocationId { get; set; }
    public TimeOnly? StartTime { get; set; } = new(19, 0);
    public int? DurationMinutes { get; set; } = 90;
    public int? MaxParticipants { get; set; } = 10;
    public int? CountryId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResources>)) as IStringLocalizer<SharedResources>;

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_NameRequired"), [nameof(Name)]);
        }

        if (MaxParticipants is <= 0)
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_MaxParticipantsInvalid"), [nameof(MaxParticipants)]);
        }

        if (DurationMinutes is <= 0)
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_DurationMinutesInvalid"), [nameof(DurationMinutes)]);
        }
    }

    private static string GetMessage(IStringLocalizer<SharedResources>? localizer, string key) => localizer is null ? key : localizer[key].Value;
}
