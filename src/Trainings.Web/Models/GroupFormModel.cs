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
    public TimeOnly? StartTime { get; set; }
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// UI-only convenience field (not persisted): derived from/deriving <see cref="StartTime"/>
    /// and <see cref="DurationMinutes"/> in <c>GroupCreateEditPage</c>. Validated as required here
    /// so the End input can show its own validation message like Start/Duration.
    /// </summary>
    public TimeOnly? EndTime { get; set; }
    public int? MaxParticipants { get; set; }
    public int? CountryId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResources>)) as IStringLocalizer<SharedResources>;

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_NameRequired"), [nameof(Name)]);
        }

        if (string.IsNullOrWhiteSpace(Slug))
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_SlugRequired"), [nameof(Slug)]);
        }

        if (CountryId is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_CountryRequired"), [nameof(CountryId)]);
        }

        if (LocationId is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_LocationRequired"), [nameof(LocationId)]);
        }

        if (Weekday is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_WeekdayRequired"), [nameof(Weekday)]);
        }

        if (StartTime is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_StartTimeRequired"), [nameof(StartTime)]);
        }

        if (EndTime is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_EndTimeRequired"), [nameof(EndTime)]);
        }

        if (MaxParticipants is null or <= 0)
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_MaxParticipantsInvalid"), [nameof(MaxParticipants)]);
        }

        if (DurationMinutes is null or <= 0)
        {
            yield return new ValidationResult(GetMessage(localizer, "GroupCreateEditPage_DurationMinutesInvalid"), [nameof(DurationMinutes)]);
        }
    }

    private static string GetMessage(IStringLocalizer<SharedResources>? localizer, string key) => localizer is null ? key : localizer[key].Value;
}
