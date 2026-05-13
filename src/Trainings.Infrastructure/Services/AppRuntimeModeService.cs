using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Infrastructure.Configuration;

namespace Trainings.Infrastructure.Services;

public class AppRuntimeModeService : IAppRuntimeModeService
{
    private readonly AppModeOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationHelper _authorizationHelper;

    public AppRuntimeModeService(
        IOptions<AppModeOptions> options,
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationHelper authorizationHelper)
    {
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _authorizationHelper = authorizationHelper;
    }

    public AppRuntimeModeDto GetCurrent() => new()
    {
        IsReadOnly = _options.ReadOnly,
        IsNoEmail = _options.NoEmail
    };

    public void EnsureWriteAllowed()
    {
        if (!_options.ReadOnly)
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
