using FluentAssertions;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Xunit;

namespace Trainings.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void UserDefaultRoleIsParticipant()
    {
        var user = new User();
        user.Role.Should().Be(UserRole.Participant);
    }

    [Fact]
    public void UserDefaultIsActiveIsTrue()
    {
        var user = new User();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UserCanSetName()
    {
        var user = new User { Name = "John Doe" };
        user.Name.Should().Be("John Doe");
    }

    [Fact]
    public void UserCanSetRole()
    {
        var user = new User { Role = UserRole.Trainer };
        user.Role.Should().Be(UserRole.Trainer);
    }
}
