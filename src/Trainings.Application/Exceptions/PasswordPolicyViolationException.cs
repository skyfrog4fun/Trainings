using Trainings.Application.Services;

namespace Trainings.Application.Exceptions;

/// <summary>
/// Thrown by <see cref="PasswordPolicy.EnsureValid"/> when a password fails the strength policy.
/// Carries the structured <see cref="PasswordValidationError"/> reason so callers (e.g. the Web layer)
/// can localize the failure instead of relying on <see cref="Exception.Message"/>, which is always English.
/// </summary>
public class PasswordPolicyViolationException : ArgumentException
{
    public PasswordValidationError Error { get; }

    public PasswordPolicyViolationException(PasswordValidationError error, string message, string paramName) : base(message, paramName)
    {
        Error = error;
    }
}
