using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;

namespace Trainings.Web.Models;

public class UserProfileFormModel : IValidatableObject
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int? CountryId { get; set; }
    public string CountryDisplay { get; set; } = string.Empty;
    public string WelcomeMessage { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResources>)) as IStringLocalizer<SharedResources>;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            yield return new ValidationResult(GetMessage(localizer, "UserInformationPage_FirstNameRequired"), new[] { nameof(FirstName) });
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            yield return new ValidationResult(GetMessage(localizer, "UserInformationPage_LastNameRequired"), new[] { nameof(LastName) });
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult(GetMessage(localizer, "UserInformationPage_EmailRequired"), new[] { nameof(Email) });
        }

        if (CountryId is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "UserInformationPage_CountryRequired"), new[] { nameof(CountryId) });
        }
    }

    private static string GetMessage(IStringLocalizer<SharedResources>? localizer, string key) => localizer is null ? key : localizer[key].Value;
}
