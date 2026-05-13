namespace Trainings.Application.DTOs;

public sealed class AppRuntimeModeDto
{
    public bool IsReadOnly { get; init; }
    public bool IsNoEmail { get; init; }
    public bool IsEmailSuppressed => IsReadOnly || IsNoEmail;

    public IReadOnlyList<string> ActiveModes => BuildActiveModes();

    private List<string> BuildActiveModes()
    {
        var activeModes = new List<string>();
        if (IsReadOnly)
        {
            activeModes.Add("Read Only");
        }

        if (IsNoEmail)
        {
            activeModes.Add("No E-Mail");
        }

        return activeModes;
    }
}
