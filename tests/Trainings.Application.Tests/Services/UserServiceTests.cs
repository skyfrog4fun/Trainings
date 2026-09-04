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
    private readonly Mock<IAppRuntimeModeService> _runtimeModeServiceMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly Mock<ICurrentUserContext> _currentUserContextMock = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(
            _userRepoMock.Object,
            _runtimeModeServiceMock.Object,
            _hasherMock.Object,
            _currentUserContextMock.Object);
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
        _hasherMock.Setup(h => h.Hash("Password1!")).Returns("hashed");
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var dto = new CreateUserDto { FirstName = "Bob", LastName = "Jones", Email = "bob@example.com", Password = "Password1!", Role = UserRole.User };
        var result = await _service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.DisplayName.Should().Be("Bob Jones");
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncSetsEntryDateWhenProvided()
    {
        _hasherMock.Setup(h => h.Hash("Password1!")).Returns("hashed");
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var entryDate = new DateTime(2026, 9, 4);
        var dto = new CreateUserDto { FirstName = "Bob", LastName = "Jones", Email = "bob@example.com", Password = "Password1!", Role = UserRole.User, EntryDate = entryDate };
        var result = await _service.CreateAsync(dto);

        result.EntryDate.Should().Be(entryDate);
    }

    [Fact]
    public async Task CreateAsyncThrowsForWeakPasswordWithoutTouchingRepository()
    {
        var dto = new CreateUserDto { FirstName = "Bob", LastName = "Jones", Email = "bob@example.com", Password = "weak", Role = UserRole.User };

        var act = () => _service.CreateAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>();
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsyncThrowsForWeakPasswordWithoutUpdatingUser()
    {
        var user = new User { Id = 3, PasswordHash = "old-hash" };
        _userRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(user);

        var act = () => _service.ChangePasswordAsync(3, "weak");

        await act.Should().ThrowAsync<ArgumentException>();
        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ValidatePasswordAsyncReturnsFalseWhenUserNotFound()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("noone@example.com")).ReturnsAsync((User?)null);
        bool result = await _service.ValidatePasswordAsync("noone@example.com", "pass");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePasswordAsyncReturnsTrueWhenPasswordCorrect()
    {
        var user = new User { Email = "user@example.com", PasswordHash = "hash", IsActive = true };
        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("pass", "hash")).Returns(true);
        bool result = await _service.ValidatePasswordAsync("user@example.com", "pass");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePasswordAsyncReturnsFalseWhenUserInactive()
    {
        var user = new User { Email = "user@example.com", PasswordHash = "hash", IsActive = false };
        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);
        bool result = await _service.ValidatePasswordAsync("user@example.com", "pass");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSelfAsyncUpdatesOnlyProfileFieldsForAuthenticatedUser()
    {
        var user = new User
        {
            Id = 7,
            FirstName = "Old",
            LastName = "Name",
            Email = "old@example.com",
            Role = UserRole.SuperAdmin,
            IsActive = false,
            EntryDate = DateTime.UtcNow.AddDays(-5)
        };
        _userRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
        _currentUserContextMock.Setup(c => c.GetCurrentUserId()).Returns(7);

        await _service.UpdateSelfAsync(new UpdateSelfUserDto
        {
            FirstName = "New",
            LastName = "User",
            Email = "new@example.com",
            City = "Basel",
            WelcomeMessage = "Hello"
        });

        user.FirstName.Should().Be("New");
        user.LastName.Should().Be("User");
        user.Email.Should().Be("new@example.com");
        user.City.Should().Be("Basel");
        user.WelcomeMessage.Should().Be("Hello");
        user.Role.Should().Be(UserRole.SuperAdmin);
        user.IsActive.Should().BeFalse();
        user.EntryDate.Should().NotBeNull();
        _userRepoMock.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateSelfAsyncThrowsWhenNoAuthenticatedUser()
    {
        _currentUserContextMock.Setup(c => c.GetCurrentUserId()).Returns((int?)null);

        var act = () => _service.UpdateSelfAsync(new UpdateSelfUserDto { FirstName = "A", LastName = "B", Email = "a@b.ch" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Authenticated user context is required.");
    }
}
