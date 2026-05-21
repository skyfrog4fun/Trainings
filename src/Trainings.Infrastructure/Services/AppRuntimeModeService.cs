using Microsoft.AspNetCore.Http;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;

namespace Trainings.Infrastructure.Services;

public class AppRuntimeModeService : IAppRuntimeModeService
{
    private readonly AppRuntimeModeState _state;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationHelper _authorizationHelper;

    public AppRuntimeModeService(
        AppRuntimeModeState state,
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationHelper authorizationHelper)
    {
        _state = state;
        _httpContextAccessor = httpContextAccessor;
        _authorizationHelper = authorizationHelper;
    }

    public AppRuntimeModeDto GetCurrent()
    {
        var (readOnly, noEmail) = _state.GetEffective();
        return new() { IsReadOnly = readOnly, IsNoEmail = noEmail };
    }

    public AppRuntimeModeDto GetDefaults()
    {
        var (readOnly, noEmail) = _state.GetDefaults();
        return new() { IsReadOnly = readOnly, IsNoEmail = noEmail };
    }

    public void SetModes(bool isReadOnly, bool isNoEmail) => _state.Set(isReadOnly, isNoEmail);

    public void ResetToDefaults() => _state.ResetToDefaults();

    public void EnsureWriteAllowed()
    {
        var (readOnly, noEmail) = _state.GetEffective();
        if (!readOnly)
        {
            return;
        }

        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true && _authorizationHelper.IsSuperAdmin(user))
        {
            return;
        }

        throw new InvalidOperationException("The application is currently in read-only mode. Only SuperAdmins can make changes.");
    }
}

