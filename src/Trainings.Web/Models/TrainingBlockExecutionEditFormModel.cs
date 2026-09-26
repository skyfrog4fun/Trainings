using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Localization;
using Trainings.Application.DTOs;

namespace Trainings.Web.Models;

public class TrainingBlockExecutionEditFormModel : IValidatableObject
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PlannedDurationMinutes { get; set; }
    public int? MinParticipants { get; set; }
    public int? MaxParticipants { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResources>)) as IStringLocalizer<SharedResources>;

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

        if (PlannedDurationMinutes is null or <= 0)
        {
            yield return new ValidationResult(GetMessage(localizer, "TrainingBlockEditor_DurationRequired"), [nameof(PlannedDurationMinutes)]);
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

    public void LoadFrom(TrainingBlockDto block)
    {
        Title = block.Title;
        Description = block.Description;
        PlannedDurationMinutes = block.PlannedDurationMinutes;
        MinParticipants = block.MinParticipants;
        MaxParticipants = block.MaxParticipants;
    }

    private static string GetMessage(IStringLocalizer<SharedResources>? localizer, string key)
        => localizer is null ? key : localizer[key].Value;
}
