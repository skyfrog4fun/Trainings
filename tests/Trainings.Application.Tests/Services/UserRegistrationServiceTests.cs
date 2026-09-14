using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;
using Trainings.Infrastructure.Services;

namespace Trainings.Application.Tests.Services;

public class UserRegistrationServiceTests
{
    private const string ValidPassword = "Passw0rd!";

    [Fact]
    public async Task RegisterAsyncSendsOneEmailPerGroupToGroupAdminsWithSuperAdminsCcd()
    {
        using var context = CreateInMemoryContext();

        var superAdmin = new User { FirstName = "Sam", LastName = "Super", Email = "superadmin@example.com", Role = UserRole.SuperAdmin, PasswordHash = "x" };
        var groupAAdmin = new User { FirstName = "Alice", LastName = "AdminA", Email = "admin-a@example.com", Role = UserRole.User, PasswordHash = "x" };
        var groupBAdmin = new User { FirstName = "Bob", LastName = "AdminB", Email = "admin-b@example.com", Role = UserRole.User, PasswordHash = "x" };
        context.Users.AddRange(superAdmin, groupAAdmin, groupBAdmin);

        var groupA = new Group { Name = "Group A", Slug = "group-a", Identifier = "GA" };
        var groupB = new Group { Name = "Group B", Slug = "group-b", Identifier = "GB" };
        context.Groups.AddRange(groupA, groupB);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.GroupMemberships.AddRange(
            new GroupMembership { UserId = groupAAdmin.Id, GroupId = groupA.Id, Role = GroupMemberRole.Admin, Status = GroupMembershipStatus.Approved, IsActive = true },
            new GroupMembership { UserId = groupBAdmin.Id, GroupId = groupB.Id, Role = GroupMemberRole.Admin, Status = GroupMembershipStatus.Approved, IsActive = true });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var emailServiceMock = CreateEmailServiceMock();
        var service = CreateService(context, emailServiceMock);

        var dto = new RegisterRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = ValidPassword,
            Gender = Gender.Male,
            RequestedGroupIds = [groupA.Id, groupB.Id]
        };

        await service.RegisterAsync(dto, TestContext.Current.CancellationToken);

        emailServiceMock.Verify(e => e.SendGroupAdminNewParticipantNotificationAsync(
            It.Is<IReadOnlyCollection<string>>(to => to.Single() == groupAAdmin.Email),
            It.Is<IReadOnlyCollection<string>>(cc => cc.Single() == superAdmin.Email),
            "John Doe",
            dto.Email,
            groupA.Id,
            groupA.Name,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        emailServiceMock.Verify(e => e.SendGroupAdminNewParticipantNotificationAsync(
            It.Is<IReadOnlyCollection<string>>(to => to.Single() == groupBAdmin.Email),
            It.Is<IReadOnlyCollection<string>>(cc => cc.Single() == superAdmin.Email),
            "John Doe",
            dto.Email,
            groupB.Id,
            groupB.Name,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        emailServiceMock.Verify(e => e.SendSuperAdminNewParticipantNotificationAsync(
            It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsyncNotifiesSuperAdminsOnlyWhenNoGroupWasRequested()
    {
        using var context = CreateInMemoryContext();

        var superAdmin = new User { FirstName = "Sam", LastName = "Super", Email = "superadmin@example.com", Role = UserRole.SuperAdmin, PasswordHash = "x" };
        context.Users.Add(superAdmin);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var emailServiceMock = CreateEmailServiceMock();
        var service = CreateService(context, emailServiceMock);

        var dto = new RegisterRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = ValidPassword,
            Gender = Gender.Male,
            RequestedGroupIds = []
        };

        await service.RegisterAsync(dto, TestContext.Current.CancellationToken);

        emailServiceMock.Verify(e => e.SendSuperAdminNewParticipantNotificationAsync(
            It.Is<IReadOnlyCollection<string>>(to => to.Single() == superAdmin.Email),
            "John Doe",
            dto.Email,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        emailServiceMock.Verify(e => e.SendGroupAdminNewParticipantNotificationAsync(
            It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsyncFallsBackToSuperAdminsWhenGroupHasNoAdmins()
    {
        using var context = CreateInMemoryContext();

        var superAdmin = new User { FirstName = "Sam", LastName = "Super", Email = "superadmin@example.com", Role = UserRole.SuperAdmin, PasswordHash = "x" };
        context.Users.Add(superAdmin);

        var groupWithoutAdmins = new Group { Name = "Group C", Slug = "group-c", Identifier = "GC" };
        context.Groups.Add(groupWithoutAdmins);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var emailServiceMock = CreateEmailServiceMock();
        var service = CreateService(context, emailServiceMock);

        var dto = new RegisterRequestDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = ValidPassword,
            Gender = Gender.Male,
            RequestedGroupIds = [groupWithoutAdmins.Id]
        };

        await service.RegisterAsync(dto, TestContext.Current.CancellationToken);

        emailServiceMock.Verify(e => e.SendGroupAdminNewParticipantNotificationAsync(
            It.Is<IReadOnlyCollection<string>>(to => to.Single() == superAdmin.Email),
            It.Is<IReadOnlyCollection<string>>(cc => cc.Count == 0),
            "John Doe",
            dto.Email,
            groupWithoutAdmins.Id,
            groupWithoutAdmins.Name,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveUserAsyncSetsEntryDateWhenNotAlreadySet()
    {
        using var context = CreateInMemoryContext();

        var newUser = new User { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Role = UserRole.User, PasswordHash = "x" };
        context.Users.Add(newUser);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var group = new Group { Name = "Group A", Slug = "group-a", Identifier = "GA" };
        context.Groups.Add(group);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.GroupMemberships.Add(new GroupMembership { UserId = newUser.Id, GroupId = group.Id, Role = GroupMemberRole.Participant, Status = GroupMembershipStatus.Pending, IsActive = false });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var emailServiceMock = CreateEmailServiceMock();
        var service = CreateService(context, emailServiceMock);

        await service.ApproveUserAsync(newUser.Id, adminUserId: 1, TestContext.Current.CancellationToken);

        newUser.EntryDate.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveUserAsyncDoesNotOverwriteExistingEntryDate()
    {
        using var context = CreateInMemoryContext();

        var existingEntryDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var existingUser = new User { FirstName = "Jane", LastName = "Doe", Email = "jane.doe@example.com", Role = UserRole.User, PasswordHash = "x", EntryDate = existingEntryDate };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var group = new Group { Name = "Group B", Slug = "group-b", Identifier = "GB" };
        context.Groups.Add(group);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.GroupMemberships.Add(new GroupMembership { UserId = existingUser.Id, GroupId = group.Id, Role = GroupMemberRole.Participant, Status = GroupMembershipStatus.Pending, IsActive = false });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var emailServiceMock = CreateEmailServiceMock();
        var service = CreateService(context, emailServiceMock);

        await service.ApproveUserAsync(existingUser.Id, adminUserId: 1, TestContext.Current.CancellationToken);

        existingUser.EntryDate.Should().Be(existingEntryDate);
    }

    private static Mock<IEmailService> CreateEmailServiceMock()
    {
        var mock = new Mock<IEmailService>();
        mock.Setup(e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult { IsSuccess = true });
        mock.Setup(e => e.SendGroupAdminNewParticipantNotificationAsync(
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult { IsSuccess = true });
        mock.Setup(e => e.SendSuperAdminNewParticipantNotificationAsync(
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult { IsSuccess = true });
        return mock;
    }

    private static UserRegistrationService CreateService(ApplicationDbContext context, Mock<IEmailService> emailServiceMock)
    {
        var appRuntimeModeServiceMock = new Mock<IAppRuntimeModeService>();
        var authorizationHelperMock = new Mock<IAuthorizationHelper>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed");
        var configuration = new ConfigurationBuilder().Build();

        return new UserRegistrationService(
            context,
            emailServiceMock.Object,
            appRuntimeModeServiceMock.Object,
            authorizationHelperMock.Object,
            httpContextAccessorMock.Object,
            passwordHasherMock.Object,
            configuration);
    }

    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }
}
