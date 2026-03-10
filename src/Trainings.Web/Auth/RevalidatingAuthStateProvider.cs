using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Trainings.Web.Auth;

public class RevalidatingAuthStateProvider : RevalidatingServerAuthenticationStateProvider
{
    public RevalidatingAuthStateProvider(ILoggerFactory loggerFactory) : base(loggerFactory) { }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
}
