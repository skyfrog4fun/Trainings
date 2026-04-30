using FluentAssertions;
using Moq;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Application.Services;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Domain.Interfaces;
using Xunit;

namespace Trainings.Application.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_userRepoMock.Object, _hasherMock.Object);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullWhenUserNotFound()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);
        var result = await _service.GetByIdAsync(1);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsyncReturnsDtoWhenUserExists()
    {
        var user = new User { Id = 1, FirstName = "Alice", LastName = "Smith", Email = "alice@example.com", Role = UserRole.User };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        var result = await _service.GetByIdAsync(1);
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Alice Smith");
        result.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task CreateAsyncCallsRepositoryAndReturnsDto()
    {
        _hasherMock.Setup(h => h.Hash("password")).Returns("hashed");
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var dto = new CreateUserDto { FirstName = "Bob", LastName = "Jones", Email = "bob@example.com", Password = "password", Role = UserRole.User };
        var result = await _service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.DisplayName.Should().Be("Bob Jones");
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task ValidatePasswordAsyncReturnsFalseWhenUserNotFound()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("noone@example.com")).ReturnsAsync((User?)null);
        var result = await _service.ValidatePasswordAsync("noone@example.com", "pass");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePasswordAsyncReturnsTrueWhenPasswordCorrect()
    {
        var user = new User { Email = "user@example.com", PasswordHash = "hash", IsActive = true };
        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("pass", "hash")).Returns(true);
        var result = await _service.ValidatePasswordAsync("user@example.com", "pass");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePasswordAsyncReturnsFalseWhenUserInactive()
    {
        var user = new User { Email = "user@example.com", PasswordHash = "hash", IsActive = false };
        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);
        var result = await _service.ValidatePasswordAsync("user@example.com", "pass");
        result.Should().BeFalse();
    }
}
