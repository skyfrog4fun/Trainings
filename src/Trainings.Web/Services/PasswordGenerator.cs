namespace Trainings.Web.Services;

/// <summary>Generates strong random passwords for use across user management pages (e.g. admin-assigned passwords and self-service password changes).</summary>
public static class PasswordGenerator
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghjkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%^&*";
    private const string All = Upper + Lower + Digits + Special;
    private const int Length = 12;

    /// <summary>Generates a random 12-character password containing at least one uppercase letter, one lowercase letter, one digit, and one special character.</summary>
    public static string Generate()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[Length];
        rng.GetBytes(bytes);

        var chars = new char[Length];
        chars[0] = Upper[bytes[0] % Upper.Length];
        chars[1] = Lower[bytes[1] % Lower.Length];
        chars[2] = Digits[bytes[2] % Digits.Length];
        chars[3] = Special[bytes[3] % Special.Length];
        for (var i = 4; i < Length; i++)
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
}
