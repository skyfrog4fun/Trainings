using FluentAssertions;
using Moq;
using Trainings.Application.Interfaces;
using Trainings.Application.Services;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Domain.Interfaces;
using Xunit;

namespace Trainings.Application.Tests.Services;

public class RegistrationServiceTests
{
    private readonly Mock<IRegistrationRepository> _regRepoMock = new();
    private readonly Mock<ITrainingRepository> _trainingRepoMock = new();
    private readonly Mock<IAppRuntimeModeService> _runtimeMock = new();
    private readonly RegistrationService _service;

    public RegistrationServiceTests()
    {
        _service = new RegistrationService(_regRepoMock.Object, _trainingRepoMock.Object, _runtimeMock.Object);
    }

    [Fact]
    public async Task RegisterAsyncSucceedsForNewTrainingRegardlessOfHowFarInTheFuture()
    {
        var training = new Training
        {
            Id = 1,
            Title = "Far Future",
            DateTime = DateTime.UtcNow.AddDays(90),
            Capacity = 10,
            Status = TrainingStatus.New,
            Registrations = []
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(training);
        _regRepoMock.Setup(r => r.GetByTrainingIdAsync(1)).ReturnsAsync([]);
        _regRepoMock.Setup(r => r.GetByUserAndTrainingAsync(99, 1)).ReturnsAsync((Registration?)null);
        _regRepoMock.Setup(r => r.AddAsync(It.IsAny<Registration>())).Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(99, 1);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsyncSucceedsWhenPlannedTrainingIsWithin4Weeks()
    {
        var training = new Training
        {
            Id = 2,
            Title = "Near Future Planned",
            DateTime = DateTime.UtcNow.AddDays(14),
            Capacity = 10,
            Status = TrainingStatus.Planned,
            Registrations = []
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(training);
        _regRepoMock.Setup(r => r.GetByTrainingIdAsync(2)).ReturnsAsync([]);
        _regRepoMock.Setup(r => r.GetByUserAndTrainingAsync(99, 2)).ReturnsAsync((Registration?)null);
        _regRepoMock.Setup(r => r.AddAsync(It.IsAny<Registration>())).Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(99, 2);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsyncSucceedsForInPlanningTraining()
    {
        var training = new Training
        {
            Id = 3,
            Title = "In Planning",
            DateTime = DateTime.UtcNow.AddDays(5),
            Capacity = 10,
            Status = TrainingStatus.InPlanning,
            Registrations = []
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(training);
        _regRepoMock.Setup(r => r.GetByTrainingIdAsync(3)).ReturnsAsync([]);
        _regRepoMock.Setup(r => r.GetByUserAndTrainingAsync(99, 3)).ReturnsAsync((Registration?)null);
        _regRepoMock.Setup(r => r.AddAsync(It.IsAny<Registration>())).Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(99, 3);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsyncThrowsWhenTrainingIsInProgress()
    {
        var training = new Training
        {
            Id = 4,
            Title = "Running Now",
            DateTime = DateTime.UtcNow.AddMinutes(-10),
            Capacity = 10,
            Status = TrainingStatus.InProgress
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(training);

        var act = () => _service.RegisterAsync(99, 4);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registration is not open for this training.");
    }

    [Fact]
    public async Task RegisterAsyncThrowsWhenTrainingIsDone()
    {
        var training = new Training
        {
            Id = 5,
            Title = "Finished",
            DateTime = DateTime.UtcNow.AddHours(-3),
            Capacity = 10,
            Status = TrainingStatus.Done
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(training);

        var act = () => _service.RegisterAsync(99, 5);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registration is not open for this training.");
    }

    [Fact]
    public async Task CancelAsyncThrowsWhenTrainingIsInProgress()
    {
        var training = new Training
        {
            Id = 6,
            Title = "Running Now",
            DateTime = DateTime.UtcNow.AddMinutes(-5),
            Capacity = 10,
            Status = TrainingStatus.InProgress
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(training);

        var act = () => _service.CancelAsync(99, 6);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registration changes are no longer allowed for this training.");
    }

    [Fact]
    public async Task CancelAsyncThrowsWhenTrainingIsDone()
    {
        var training = new Training
        {
            Id = 7,
            Title = "Past Training",
            DateTime = DateTime.UtcNow.AddHours(-2),
            Capacity = 10,
            Status = TrainingStatus.Done
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(training);

        var act = () => _service.CancelAsync(99, 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registration changes are no longer allowed for this training.");
    }

    [Fact]
    public async Task CancelAsyncSucceedsWhenTrainingIsPlanned()
    {
        var training = new Training
        {
            Id = 8,
            Title = "Upcoming",
            DateTime = DateTime.UtcNow.AddDays(3),
            Capacity = 10,
            Status = TrainingStatus.Planned
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(training);
        _regRepoMock.Setup(r => r.GetByUserAndTrainingAsync(99, 8)).ReturnsAsync(new Registration { UserId = 99, TrainingId = 8, Status = RegistrationStatus.Registered });
        _regRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Registration>())).Returns(Task.CompletedTask);

        var act = () => _service.CancelAsync(99, 8);

        await act.Should().NotThrowAsync();
    }
}
