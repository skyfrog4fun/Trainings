using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;

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
            yield return new ValidationResult(GetMessage(localizer, "RegisterPage_FirstNameRequired"), new[] { nameof(FirstName) });
        }
        if (string.IsNullOrWhiteSpace(LastName))
        {
            yield return new ValidationResult(GetMessage(localizer, "RegisterPage_LastNameRequired"), new[] { nameof(LastName) });
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult(GetMessage(localizer, "RegisterPage_EmailRequired"), new[] { nameof(Email) });
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
        {
            yield return new ValidationResult(GetMessage(localizer, "RegisterPage_PasswordTooShort"), new[] { nameof(Password) });
        }

        if (CountryId is null)
        {
            yield return new ValidationResult(GetMessage(localizer, "RegisterPage_CountryRequired"), new[] { nameof(CountryId) });
        }
    }

    private static string GetMessage(IStringLocalizer<SharedResources>? localizer, string key) => localizer is null ? key : localizer[key].Value;
}
