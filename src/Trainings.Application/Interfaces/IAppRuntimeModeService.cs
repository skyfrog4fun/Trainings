using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface IAppRuntimeModeService
{
    AppRuntimeModeDto GetCurrent();
    AppRuntimeModeDto GetDefaults();
    void SetModes(bool isReadOnly, bool isNoEmail);
    void ResetToDefaults();
    void EnsureWriteAllowed();
}
