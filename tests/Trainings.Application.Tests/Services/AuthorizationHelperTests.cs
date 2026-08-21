using System.Security.Claims;
using FluentAssertions;
using Trainings.Infrastructure.Auth;

namespace Trainings.Application.Tests.Services;

public class AuthorizationHelperTests
{
    private readonly AuthorizationHelper _helper = new();

    [Fact]
    public void IsGroupMemberWithParticipantClaimReturnsTrue()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("GroupRole::7", "Participant")
        ], "test"));

        bool result = _helper.IsGroupMember(principal, 7);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsGroupMemberWithDifferentGroupClaimReturnsFalse()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("GroupRole::8", "Participant")
        ], "test"));

        bool result = _helper.IsGroupMember(principal, 7);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsGroupMemberWithSuperAdminClaimReturnsTrue()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("SuperAdmin", "true")
        ], "test"));

        bool result = _helper.IsGroupMember(principal, 99);

        result.Should().BeTrue();
    }
}
