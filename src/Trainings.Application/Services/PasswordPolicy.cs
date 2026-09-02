using Trainings.Application.Exceptions;

namespace Trainings.Application.Services;

/// <summary>Identifies which rule of <see cref="PasswordPolicy"/> a password failed, so callers can localize or describe the failure.</summary>
public enum PasswordValidationError
{
    None,
    TooShort,
    MissingUppercase,
    MissingLowercase,
    MissingDigit,
    MissingSpecialCharacter
}

/// <summary>
/// Single source of truth for password strength rules and generation, shared by the Web UI (client-side validation)
/// and the Application/Infrastructure services that persist passwords (server-side enforcement).
/// </summary>
public static class PasswordPolicy
{
    private const int MinLength = 8;
    private const int GeneratedLength = 12;

    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghjkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%^&*()-_=+[]{}|;:',.<>?";
    private const string All = Upper + Lower + Digits + Special;

    /// <summary>Generates a random 12-character password containing at least one uppercase letter, one lowercase letter, one digit, and one special character.</summary>
    public static string Generate()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[GeneratedLength];
        rng.GetBytes(bytes);

        var chars = new char[GeneratedLength];
        chars[0] = Upper[bytes[0] % Upper.Length];
        chars[1] = Lower[bytes[1] % Lower.Length];
        chars[2] = Digits[bytes[2] % Digits.Length];
        chars[3] = Special[bytes[3] % Special.Length];
        for (var i = 4; i < GeneratedLength; i++)
        {
            chars[i] = All[bytes[i] % All.Length];
        }

        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = bytes[i % bytes.Length] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    /// <summary>
    /// Validates that a password meets the same strength rules enforced by <see cref="Generate"/>:
    /// at least <see cref="MinLength"/> characters, one uppercase letter, one lowercase letter, one digit, and one special character.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="error">The first validation rule that failed, or <see cref="PasswordValidationError.None"/> when valid.</param>
    /// <returns><c>true</c> when the password satisfies all rules; otherwise <c>false</c>.</returns>
    public static bool Validate(string password, out PasswordValidationError error)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (password.Length < MinLength)
        {
            error = PasswordValidationError.TooShort;
            return false;
        }

        if (!password.Any(char.IsUpper))
        {
            error = PasswordValidationError.MissingUppercase;
            return false;
        }

        if (!password.Any(char.IsLower))
        {
            error = PasswordValidationError.MissingLowercase;
            return false;
        }

        if (!password.Any(char.IsDigit))
        {
            error = PasswordValidationError.MissingDigit;
            return false;
        }

        if (!password.Any(c => Special.Contains(c)))
        {
            error = PasswordValidationError.MissingSpecialCharacter;
            return false;
        }

        error = PasswordValidationError.None;
        return true;
    }

    /// <summary>Returns a plain-English description of a <see cref="PasswordValidationError"/>, for use in non-localized backend exception messages.</summary>
    public static string Describe(PasswordValidationError error) => error switch
    {
        PasswordValidationError.TooShort => $"Password must be at least {MinLength} characters.",
        PasswordValidationError.MissingUppercase => "Password must contain at least one uppercase letter.",
        PasswordValidationError.MissingLowercase => "Password must contain at least one lowercase letter.",
        PasswordValidationError.MissingDigit => "Password must contain at least one digit.",
        PasswordValidationError.MissingSpecialCharacter => "Password must contain at least one special character.",
        _ => "Password does not meet the required strength policy."
    };

    /// <summary>
    /// Server-side enforcement gate: throws <see cref="PasswordPolicyViolationException"/> when <paramref name="password"/> does not satisfy <see cref="Validate"/>.
    /// Intended for Application/Infrastructure services that persist a new password, independent of any client-side validation.
    /// </summary>
    public static void EnsureValid(string password, string paramName)
    {
        if (!Validate(password, out var error))
        {
            throw new PasswordPolicyViolationException(error, Describe(error), paramName);
        }
    }
}
