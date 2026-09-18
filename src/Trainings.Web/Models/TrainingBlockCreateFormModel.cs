using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace Trainings.Web.Models;

public class TrainingBlockCreateFormModel : IValidatableObject
{
    public int? TagId { get; set; }
    public int? GameId { get; set; }
    public string NewGameName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DurationMinutes { get; set; }
    public int? MinParticipants { get; set; }
    public int? MaxParticipants { get; set; }
    internal bool RequiresGame { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResources>)) as IStringLocalizer<SharedResources>;

        if (TagId is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "TrainingBlockEditor_TagRequired"), [nameof(TagId)]);
        }

        if (RequiresGame && GameId is null && string.IsNullOrWhiteSpace(NewGameName))
        {
            yield return new ValidationResult(GetMessage(localizer, "TrainingBlockEditor_GameRequired"), [nameof(GameId), nameof(NewGameName)]);
        }

        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult(GetMessage(localizer, "TrainingBlockEditor_TitleRequired"), [nameof(Title)]);
        }
        else if (Title.Trim().Length > 64)
        {
            yield return new ValidationResult(GetMessage(localizer, "TrainingBlockEditor_TitleTooLong"), [nameof(Title)]);
        }

        if (!string.IsNullOrWhiteSpace(Description) && Description.Trim().Length > 512)
        {
            yield return new ValidationResult(GetMessage(localizer, "TrainingBlockEditor_DescriptionTooLong"), [nameof(Description)]);
        }

        if (DurationMinutes is null or <= 0)
        {
            yield return new ValidationResult(GetMessage(localizer, "TrainingBlockEditor_DurationRequired"), [nameof(DurationMinutes)]);
        }

        if (MinParticipants is null or < 1)
        {
            yield return new ValidationResult(GetMessage(localizer, "TrainingBlockEditor_MinParticipantsRequired"), [nameof(MinParticipants)]);
        }

        if (MaxParticipants is null or < 1)
        {
            yield return new ValidationResult(GetMessage(localizer, "TrainingBlockEditor_MaxParticipantsRequired"), [nameof(MaxParticipants)]);
        }
        else if (MinParticipants.HasValue && MaxParticipants < MinParticipants)
        {
            yield return new ValidationResult(GetMessage(localizer, "TrainingBlockEditor_MinMaxParticipantsInvalid"), [nameof(MinParticipants), nameof(MaxParticipants)]);
        }
    }

    public void Reset()
    {
        TagId = null;
        GameId = null;
        NewGameName = string.Empty;
        Title = string.Empty;
        Description = null;
        DurationMinutes = null;
        MinParticipants = null;
        MaxParticipants = null;
        RequiresGame = false;
    }

    private static string GetMessage(IStringLocalizer<SharedResources>? localizer, string key)
        => localizer is null ? key : localizer[key].Value;
}
