using Trainings.Application.Services;

namespace Trainings.Web.Services;

/// <summary>
/// Maps <see cref="PasswordValidationError"/> values from the shared <see cref="PasswordPolicy"/> to Web resource keys,
/// keeping localization concerns in the Web layer while the password rules themselves live in Trainings.Application.
/// </summary>
public static class PasswordPolicyLocalization
{
    public static string GetResourceKey(PasswordValidationError error) => error switch
    {
        PasswordValidationError.TooShort => "Shared_PasswordValidationMinLength",
        PasswordValidationError.MissingUppercase => "Shared_PasswordValidationUppercase",
        PasswordValidationError.MissingLowercase => "Shared_PasswordValidationLowercase",
        PasswordValidationError.MissingDigit => "Shared_PasswordValidationDigit",
        PasswordValidationError.MissingSpecialCharacter => "Shared_PasswordValidationSpecialCharacter",
        _ => string.Empty
    };
}
