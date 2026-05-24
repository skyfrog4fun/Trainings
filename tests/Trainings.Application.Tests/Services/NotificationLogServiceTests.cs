using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;
using Trainings.Infrastructure.Services;

namespace Trainings.Application.Tests.Services;

public class NotificationLogServiceTests
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
    public async Task SetResetPointerLogIdAsync_PersistsAndReturnsPointer()
    {
        using var context = CreateInMemoryContext();
        var service = new NotificationLogService(context);

        await service.SetResetPointerLogIdAsync(42);
        var pointer = await service.GetResetPointerLogIdAsync();

        pointer.Should().Be(42);
    }

    [Fact]
    public async Task GetRecentLogsAsync_FiltersByPointer()
    {
        using var context = CreateInMemoryContext();
        context.NotificationLogs.AddRange(
            new NotificationLog { Id = 1, Action = NotificationAction.TestEmail, RecipientEmail = "a@example.com", IsSuccess = true, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new NotificationLog { Id = 2, Action = NotificationAction.TestEmail, RecipientEmail = "b@example.com", IsSuccess = false, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new NotificationLog { Id = 3, Action = NotificationAction.TestEmail, RecipientEmail = "c@example.com", IsSuccess = true, CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new NotificationLogService(context);
        var logs = await service.GetRecentLogsAsync(10, 1);

        logs.Select(l => l.Id).Should().BeEquivalentTo([3, 2]);
    }
}
