using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Infrastructure.Data;
using Trainings.Infrastructure.Services;

namespace Trainings.Application.Tests.Services;

public class SmtpEmailServiceTests
{
    [Fact]
    public async Task SendTestEmailAsyncAllConfigurationsPreservesOrderedFailuresBeforeSuccess()
    {
        using var context = CreateInMemoryContext();
        var configs = await SeedMailConfigurationsAsync(context);
        var service = CreateService(
            context,
            new Dictionary<int, SendOutcome>
            {
                [configs[0].Id] = SendOutcome.Failure("Primary failed"),
                [configs[1].Id] = SendOutcome.Success()
            });

        var result = await service.SendTestEmailAsync("user@example.com", ct: TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Attempts.Should().HaveCount(2);
        result.Attempts[0].ConfigurationName.Should().Be("Primary");
        result.Attempts[0].IsSuccess.Should().BeFalse();
        result.Attempts[0].Message.Should().Contain("Primary failed");
        result.Attempts[1].ConfigurationName.Should().Be("Secondary");
        result.Attempts[1].IsSuccess.Should().BeTrue();

        var updatedPrimary = await context.MailConfigurations.SingleAsync(c => c.Id == configs[0].Id, TestContext.Current.CancellationToken);
        updatedPrimary.Status.Should().Be(MailConfigurationStatus.Failed);
        updatedPrimary.LastError.Should().Be("Primary failed");
        updatedPrimary.LastSuccessSentAt.Should().BeNull();

        var updatedSecondary = await context.MailConfigurations.SingleAsync(c => c.Id == configs[1].Id, TestContext.Current.CancellationToken);
        updatedSecondary.Status.Should().Be(MailConfigurationStatus.Successful);
        updatedSecondary.LastError.Should().BeNull();
        updatedSecondary.LastSuccessSentAt.Should().NotBeNull();

        var untouchedTertiary = await context.MailConfigurations.SingleAsync(c => c.Id == configs[2].Id, TestContext.Current.CancellationToken);
        untouchedTertiary.Status.Should().Be(MailConfigurationStatus.Unknown);
    }

    [Fact]
    public async Task SendTestEmailAsyncSelectedInactiveConfigurationUsesOnlyThatConfiguration()
    {
        using var context = CreateInMemoryContext();
        var configs = await SeedMailConfigurationsAsync(context);
        configs[2].IsActive = false;
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = CreateService(
            context,
            new Dictionary<int, SendOutcome>
            {
                [configs[2].Id] = SendOutcome.Success()
            });

        var result = await service.SendTestEmailAsync("user@example.com", configs[2].Id, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Attempts.Should().HaveCount(1);
        result.Attempts[0].MailConfigurationId.Should().Be(configs[2].Id);
        result.Attempts[0].IsActive.Should().BeFalse();

        var inactiveConfig = await context.MailConfigurations.SingleAsync(c => c.Id == configs[2].Id, TestContext.Current.CancellationToken);
        inactiveConfig.Status.Should().Be(MailConfigurationStatus.Successful);
        inactiveConfig.LastSuccessSentAt.Should().NotBeNull();

        var untouchedPrimary = await context.MailConfigurations.SingleAsync(c => c.Id == configs[0].Id, TestContext.Current.CancellationToken);
        untouchedPrimary.Status.Should().Be(MailConfigurationStatus.Unknown);
    }

    private static TestableSmtpEmailService CreateService(
        ApplicationDbContext context,
        IReadOnlyDictionary<int, SendOutcome> outcomes)
    {
        var appRuntimeModeServiceMock = new Mock<IAppRuntimeModeService>();
        appRuntimeModeServiceMock
            .Setup(service => service.GetCurrent())
            .Returns(new AppRuntimeModeDto());
        var mailConfigService = new MailConfigurationService(context, appRuntimeModeServiceMock.Object);
        var notificationLogService = new NotificationLogService(context);
        var logger = Mock.Of<ILogger<SmtpEmailService>>();

        return new TestableSmtpEmailService(mailConfigService, notificationLogService, appRuntimeModeServiceMock.Object, logger, outcomes);
    }

    private static async Task<List<MailConfiguration>> SeedMailConfigurationsAsync(ApplicationDbContext context)
    {
        var configs = new List<MailConfiguration>
        {
            new() { Name = "Primary", Host = "smtp-primary", Port = 25, Username = "primary", Password = "secret", FromAddress = "primary@example.com", Priority = 1, IsActive = true },
            new() { Name = "Secondary", Host = "smtp-secondary", Port = 26, Username = "secondary", Password = "secret", FromAddress = "secondary@example.com", Priority = 2, IsActive = true },
            new() { Name = "Tertiary", Host = "smtp-tertiary", Port = 27, Username = "tertiary", Password = "secret", FromAddress = "tertiary@example.com", Priority = 3, IsActive = true }
        };

        context.MailConfigurations.AddRange(configs);
        await context.SaveChangesAsync();
        return configs;
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

    private sealed class TestableSmtpEmailService(
        MailConfigurationService mailConfigService,
        NotificationLogService notificationLogService,
        IAppRuntimeModeService appRuntimeModeService,
        ILogger<SmtpEmailService> logger,
        IReadOnlyDictionary<int, SmtpEmailServiceTests.SendOutcome> outcomes) : SmtpEmailService(mailConfigService, notificationLogService, appRuntimeModeService, logger)
    {
        private readonly IReadOnlyDictionary<int, SendOutcome> _outcomes = outcomes;

        protected override Task SendViaConfigAsync(MailConfiguration config, string toEmail, string subject, string htmlBody, CancellationToken ct)
        {
            if (_outcomes.TryGetValue(config.Id, out var outcome))
            {
                if (outcome.IsSuccess)
                {
                    return Task.CompletedTask;
                }

                throw new InvalidOperationException(outcome.ErrorMessage);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class SendOutcome
    {
        public bool IsSuccess { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;

        public static SendOutcome Success() => new() { IsSuccess = true };

        public static SendOutcome Failure(string errorMessage) => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
