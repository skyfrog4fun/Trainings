using FluentAssertions;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Xunit;

namespace Trainings.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void User_DefaultRole_IsParticipant()
    {
        var user = new User();
        user.Role.Should().Be(UserRole.Participant);
    }

    [Fact]
    public void User_DefaultIsActive_IsTrue()
    {
        var user = new User();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void User_CanSetName()
    {
        var user = new User { Name = "John Doe" };
        user.Name.Should().Be("John Doe");
    }

    [Fact]
    public void User_CanSetRole()
    {
        var user = new User { Role = UserRole.Trainer };
        user.Role.Should().Be(UserRole.Trainer);
    }
}
