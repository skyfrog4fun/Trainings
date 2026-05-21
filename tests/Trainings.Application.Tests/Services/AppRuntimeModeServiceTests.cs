using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Trainings.Application.Interfaces;
using Trainings.Infrastructure.Configuration;
using Trainings.Infrastructure.Services;
using Xunit;

namespace Trainings.Application.Tests.Services;

public class AppRuntimeModeServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly Mock<IAuthorizationHelper> _authorizationHelperMock = new();

    private static AppRuntimeModeState CreateState(bool readOnly = false, bool noEmail = false)
        => new(new AppModeOptions { ReadOnly = readOnly, NoEmail = noEmail });

    private AppRuntimeModeService CreateService(AppRuntimeModeState state)
        => new(state, _httpContextAccessorMock.Object, _authorizationHelperMock.Object);

    [Fact]
    public void GetCurrentReturnsConfigDefaultsWhenNotOverridden()
    {
        var state = CreateState(readOnly: true, noEmail: false);
        var service = CreateService(state);

        var result = service.GetCurrent();

        result.IsReadOnly.Should().BeTrue();
        result.IsNoEmail.Should().BeFalse();
    }

    [Fact]
    public void GetDefaultsAlwaysReturnsConfigDefaultsEvenAfterOverride()
    {
        var state = CreateState(readOnly: false, noEmail: false);
        var service = CreateService(state);

        service.SetModes(isReadOnly: true, isNoEmail: true);

        var defaults = service.GetDefaults();
        defaults.IsReadOnly.Should().BeFalse();
        defaults.IsNoEmail.Should().BeFalse();
    }

    [Fact]
    public void SetModesUpdatesEffectiveState()
    {
        var state = CreateState(readOnly: false, noEmail: false);
        var service = CreateService(state);

        service.SetModes(isReadOnly: true, isNoEmail: true);

        var result = service.GetCurrent();
        result.IsReadOnly.Should().BeTrue();
        result.IsNoEmail.Should().BeTrue();
    }

    [Fact]
    public void ResetToDefaultsRestoresConfigValues()
    {
        var state = CreateState(readOnly: false, noEmail: false);
        var service = CreateService(state);

        service.SetModes(isReadOnly: true, isNoEmail: true);
        service.ResetToDefaults();

        var result = service.GetCurrent();
        result.IsReadOnly.Should().BeFalse();
        result.IsNoEmail.Should().BeFalse();
    }

    [Fact]
    public void ResetToDefaultsRestoresTrueDefaults()
    {
        var state = CreateState(readOnly: true, noEmail: true);
        var service = CreateService(state);

        service.SetModes(isReadOnly: false, isNoEmail: false);
        service.ResetToDefaults();

        var result = service.GetCurrent();
        result.IsReadOnly.Should().BeTrue();
        result.IsNoEmail.Should().BeTrue();
    }

    [Fact]
    public void EnsureWriteAllowedDoesNotThrowWhenNotReadOnly()
    {
        var state = CreateState(readOnly: false);
        var service = CreateService(state);

        var act = service.EnsureWriteAllowed;

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureWriteAllowedThrowsWhenReadOnlyAndUserNotAuthenticated()
    {
        var state = CreateState(readOnly: true);
        var service = CreateService(state);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var act = service.EnsureWriteAllowed;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*read-only mode*");
    }

    [Fact]
    public void EnsureWriteAllowedDoesNotThrowWhenReadOnlyAndUserIsSuperAdmin()
    {
        var state = CreateState(readOnly: true);
        var service = CreateService(state);

        var identity = new ClaimsIdentity([new Claim("SuperAdmin", "true")], "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _authorizationHelperMock.Setup(x => x.IsSuperAdmin(principal)).Returns(true);

        var act = service.EnsureWriteAllowed;

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureWriteAllowedThrowsWhenReadOnlyAndUserIsNotSuperAdmin()
    {
        var state = CreateState(readOnly: true);
        var service = CreateService(state);

        var identity = new ClaimsIdentity([new Claim("sub", "1")], "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _authorizationHelperMock.Setup(x => x.IsSuperAdmin(principal)).Returns(false);

        var act = service.EnsureWriteAllowed;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*read-only mode*");
    }

    [Fact]
    public void EnsureWriteAllowedDoesNotThrowAfterSetModesDisablesReadOnly()
    {
        var state = CreateState(readOnly: true);
        var service = CreateService(state);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        service.SetModes(isReadOnly: false, isNoEmail: false);

        var act = service.EnsureWriteAllowed;

        act.Should().NotThrow();
    }

    [Fact]
    public void IsEmailSuppressedIsTrueWhenEitherModeActive()
    {
        var state = CreateState(readOnly: false, noEmail: false);
        var service = CreateService(state);

        service.SetModes(isReadOnly: false, isNoEmail: true);
        service.GetCurrent().IsEmailSuppressed.Should().BeTrue();

        service.SetModes(isReadOnly: true, isNoEmail: false);
        service.GetCurrent().IsEmailSuppressed.Should().BeTrue();

        service.SetModes(isReadOnly: false, isNoEmail: false);
        service.GetCurrent().IsEmailSuppressed.Should().BeFalse();
    }
}
