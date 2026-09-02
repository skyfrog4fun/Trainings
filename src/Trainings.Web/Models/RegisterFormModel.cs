using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;

using Trainings.Application.Services;
using Trainings.Web.Services;

namespace Trainings.Web.Models;

public class RegisterFormModel : IValidatableObject
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Gender { get; set; } = "Other";
    public DateTime? Birthday { get; set; }
    public string Mobile { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int? CountryId { get; set; }
    public string WelcomeMessage { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResources>)) as IStringLocalizer<SharedResources>;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            yield return new ValidationResult(GetMessage(localizer, "RegisterPage_FirstNameRequired"), [nameof(FirstName)]);
        }
        if (string.IsNullOrWhiteSpace(LastName))
        {
            yield return new ValidationResult(GetMessage(localizer, "RegisterPage_LastNameRequired"), [nameof(LastName)]);
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult(GetMessage(localizer, "RegisterPage_EmailRequired"), [nameof(Email)]);
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult(GetMessage(localizer, "RegisterPage_PasswordRequired"), [nameof(Password)]);
        }
        else if (!PasswordPolicy.Validate(Password, out var passwordError))
        {
            yield return new ValidationResult(GetMessage(localizer, PasswordPolicyLocalization.GetResourceKey(passwordError)), [nameof(Password)]);
        }

        if (CountryId is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "RegisterPage_CountryRequired"), [nameof(CountryId)]);
        }
    }

    private static string GetMessage(IStringLocalizer<SharedResources>? localizer, string key) => localizer is null ? key : localizer[key].Value;
}
