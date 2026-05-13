using Trainings.Application.DTOs;

namespace Trainings.Application.Interfaces;

public interface IAppRuntimeModeService
{
    AppRuntimeModeDto GetCurrent();
    void EnsureWriteAllowed();
}
