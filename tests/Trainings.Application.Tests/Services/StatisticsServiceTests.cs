using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;
using Trainings.Infrastructure.Services;

namespace Trainings.Application.Tests.Services;

public class StatisticsServiceTests
{
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

    [Fact]
    public async Task GetStatisticsAsync_SuperAdmin_ReturnsGlobalMetrics()
    {
        using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = new StatisticsService(context);

        var result = await service.GetStatisticsAsync(true, []);

        result.TotalGroups.Should().Be(2);
        result.ActiveUsers.Should().Be(2);
        result.TotalTrainings.Should().Be(2);
        result.TotalRegistrations.Should().Be(2);
        result.UsersNotInGroup.Should().Be(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_GroupAdmin_ReturnsScopedMetrics()
    {
        using var context = CreateInMemoryContext();
        await SeedAsync(context);
        var service = new StatisticsService(context);

        var result = await service.GetStatisticsAsync(false, [1]);

        result.TotalGroups.Should().Be(1);
        result.ActiveUsers.Should().Be(1);
        result.TotalTrainings.Should().Be(1);
        result.TotalRegistrations.Should().Be(1);
        result.UsersNotInGroup.Should().Be(0);
    }

    private static async Task SeedAsync(ApplicationDbContext context)
    {
        var user1 = new User { Id = 1, FirstName = "Alice", LastName = "Admin", Email = "alice@example.com", PasswordHash = "x", IsActive = true };
        var user2 = new User { Id = 2, FirstName = "Bob", LastName = "Member", Email = "bob@example.com", PasswordHash = "x", IsActive = true };
        var user3 = new User { Id = 3, FirstName = "Carl", LastName = "Inactive", Email = "carl@example.com", PasswordHash = "x", IsActive = false };

        var group1 = new Group { Id = 1, Name = "Group 1", Slug = "group-1", Identifier = "g1", CreatedAt = DateTime.UtcNow, IsActive = true };
        var group2 = new Group { Id = 2, Name = "Group 2", Slug = "group-2", Identifier = "g2", CreatedAt = DateTime.UtcNow, IsActive = true };

        var training1 = new Training { Id = 1, Title = "Training 1", DateTime = DateTime.UtcNow.AddDays(5), Capacity = 10, TrainerId = 1, GroupId = 1, IsActive = true };
        var training2 = new Training { Id = 2, Title = "Training 2", DateTime = DateTime.UtcNow.AddDays(10), Capacity = 10, TrainerId = 1, GroupId = 2, IsActive = true };

        context.Users.AddRange(user1, user2, user3);
        context.Groups.AddRange(group1, group2);
        await context.SaveChangesAsync();

        context.GroupMemberships.AddRange(
            new GroupMembership { UserId = 1, GroupId = 1, Role = GroupMemberRole.Admin, Status = GroupMembershipStatus.Approved, IsActive = true },
            new GroupMembership { UserId = 2, GroupId = 2, Role = GroupMemberRole.Participant, Status = GroupMembershipStatus.Approved, IsActive = true });

        context.Trainings.AddRange(training1, training2);
        await context.SaveChangesAsync();

        context.Registrations.AddRange(
            new Registration { UserId = 1, TrainingId = 1, Status = RegistrationStatus.Registered, RegisteredAt = DateTime.UtcNow },
            new Registration { UserId = 2, TrainingId = 2, Status = RegistrationStatus.Registered, RegisteredAt = DateTime.UtcNow });

        await context.SaveChangesAsync();
    }
}
