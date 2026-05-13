namespace Trainings.Application.DTOs;

public sealed class AppRuntimeModeDto
{
    public bool IsReadOnly { get; init; }
    public bool IsNoEmail { get; init; }
    public bool IsEmailSuppressed => IsReadOnly || IsNoEmail;

    public IReadOnlyList<string> ActiveModes =>
        [
            ..(IsReadOnly ? ["Read Only"] : []),
            ..(IsNoEmail ? ["No E-Mail"] : [])
        ];
}
