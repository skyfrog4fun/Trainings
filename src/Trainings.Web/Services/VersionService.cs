namespace Trainings.Web.Services;

public sealed class VersionService
{
    public string ApplicationVersion { get; } = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
}
