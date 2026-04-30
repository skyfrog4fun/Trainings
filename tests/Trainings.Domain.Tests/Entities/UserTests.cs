using FluentAssertions;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Xunit;

namespace Trainings.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void UserDefaultRoleIsUser()
    {
        var user = new User();
        user.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public void UserDefaultIsActiveIsTrue()
    {
        var user = new User();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UserDisplayNameCombinesFirstAndLastName()
    {
        var user = new User { FirstName = "John", LastName = "Doe" };
        user.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public void UserCanSetRole()
    {
        var user = new User { Role = UserRole.SuperAdmin };
        user.Role.Should().Be(UserRole.SuperAdmin);
    }
}
