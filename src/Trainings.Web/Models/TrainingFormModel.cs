using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;

namespace Trainings.Web.Models;

public class TrainingFormModel : IValidatableObject
{
    public int? GroupId { get; set; }
    public int TrainerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public int? LocationId { get; set; }
    public int? Capacity { get; set; }
    public TimeOnly? StartTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string SpecialLocationDescription { get; set; } = string.Empty;
    public string MeetingPoint { get; set; } = string.Empty;

    /// <summary>
    /// Set by the page to the training's original group when editing. Used to detect an
    /// attempted group change once the training already has registered participants.
    /// </summary>
    internal int? OriginalGroupId { get; set; }

    /// <summary>
    /// Set by the page when the training already has one or more registered participants,
    /// which locks the Group selection.
    /// </summary>
    internal bool HasParticipants { get; set; }

    /// <summary>
    /// Set by the page when the selected location is a "Special"/"Outside" location, which
    /// makes <see cref="SpecialLocationDescription"/> and <see cref="MeetingPoint"/> mandatory.
    /// </summary>
    internal bool IsSpecialLocationSelected { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResources>)) as IStringLocalizer<SharedResources>;

        if (GroupId is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "CreateEditTrainingPage_GroupRequired"), [nameof(GroupId)]);
        }

        if (Date is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "CreateEditTrainingPage_DateRequired"), [nameof(Date)]);
        }

        if (LocationId is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "CreateEditTrainingPage_LocationRequired"), [nameof(LocationId)]);
        }

        if (Capacity is null or <= 0)
        {
            yield return new ValidationResult(GetMessage(localizer, "CreateEditTrainingPage_CapacityRequired"), [nameof(Capacity)]);
        }

        if (StartTime is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "CreateEditTrainingPage_StartRequired"), [nameof(StartTime)]);
        }

        if (DurationMinutes is null or <= 0)
        {
            yield return new ValidationResult(GetMessage(localizer, "CreateEditTrainingPage_DurationRequired"), [nameof(DurationMinutes)]);
        }

        if (HasParticipants && OriginalGroupId.HasValue && GroupId != OriginalGroupId)
        {
            yield return new ValidationResult(GetMessage(localizer, "CreateEditTrainingPage_GroupLockedHasParticipants"), [nameof(GroupId)]);
        }

        if (IsSpecialLocationSelected)
        {
            if (string.IsNullOrWhiteSpace(SpecialLocationDescription))
            {
                yield return new ValidationResult(GetMessage(localizer, "CreateEditTrainingPage_SpecialLocationRequired"), [nameof(SpecialLocationDescription)]);
            }

            if (string.IsNullOrWhiteSpace(MeetingPoint))
            {
                yield return new ValidationResult(GetMessage(localizer, "CreateEditTrainingPage_MeetingPointRequired"), [nameof(MeetingPoint)]);
            }
        }
    }

    private static string GetMessage(IStringLocalizer<SharedResources>? localizer, string key) => localizer is null ? key : localizer[key].Value;
}
