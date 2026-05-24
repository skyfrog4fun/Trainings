using System.Security.Claims;
using FluentAssertions;
using Trainings.Infrastructure.Auth;

namespace Trainings.Application.Tests.Services;

public class AuthorizationHelperTests
{
    private readonly AuthorizationHelper _helper = new();

    [Fact]
    public void IsGroupMember_WithParticipantClaim_ReturnsTrue()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("GroupRole::7", "Participant")
        ], "test"));

        var result = _helper.IsGroupMember(principal, 7);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsGroupMember_WithDifferentGroupClaim_ReturnsFalse()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("GroupRole::8", "Participant")
        ], "test"));

        var result = _helper.IsGroupMember(principal, 7);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsGroupMember_WithSuperAdminClaim_ReturnsTrue()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("SuperAdmin", "true")
        ], "test"));

        var result = _helper.IsGroupMember(principal, 99);

        result.Should().BeTrue();
    }
}
