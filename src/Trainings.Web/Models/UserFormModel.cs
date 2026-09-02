using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;

using Trainings.Application.Services;
using Trainings.Web.Services;

namespace Trainings.Web.Models;

public class UserFormModel : IValidatableObject
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string Gender { get; set; } = "Other";
    public DateTime? Birthday { get; set; }
    public string Mobile { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int? CountryId { get; set; }
    public string WelcomeMessage { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public DateTime? EntryDate { get; set; }
    public bool IsActive { get; set; } = true;

    internal bool IsEdit { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedResources>)) as IStringLocalizer<SharedResources>;

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            yield return new ValidationResult(GetMessage(localizer, "UserCreateEditPage_FirstNameRequired"), [nameof(FirstName)]);
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            yield return new ValidationResult(GetMessage(localizer, "UserCreateEditPage_LastNameRequired"), [nameof(LastName)]);
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult(GetMessage(localizer, "UserCreateEditPage_EmailRequired"), [nameof(Email)]);
        }
        else if (!new EmailAddressAttribute().IsValid(Email))
        {
            yield return new ValidationResult(GetMessage(localizer, "UserCreateEditPage_EmailInvalid"), [nameof(Email)]);
        }

        if (!IsEdit)
        {
            if (string.IsNullOrWhiteSpace(Password))
            {
                yield return new ValidationResult(GetMessage(localizer, "UserCreateEditPage_PasswordRequired"), [nameof(Password)]);
            }
            else if (!PasswordPolicy.Validate(Password, out var passwordError))
            {
                yield return new ValidationResult(GetMessage(localizer, PasswordPolicyLocalization.GetResourceKey(passwordError)), [nameof(Password)]);
            }
        }

        if (IsEdit && !string.IsNullOrWhiteSpace(NewPassword) && !PasswordPolicy.Validate(NewPassword, out var newPasswordError))
        {
            yield return new ValidationResult(GetMessage(localizer, PasswordPolicyLocalization.GetResourceKey(newPasswordError)), [nameof(NewPassword)]);
        }
    }

    private static string GetMessage(IStringLocalizer<SharedResources>? localizer, string key) => localizer is null ? key : localizer[key].Value;
}
