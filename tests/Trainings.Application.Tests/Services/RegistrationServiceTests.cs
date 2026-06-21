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
    public async Task RegisterAsyncThrowsWhenPlannedTrainingIsBeyond4Weeks()
    {
        var training = new Training
        {
            Id = 1,
            Title = "Far Future",
            DateTime = DateTime.UtcNow.AddDays(29),
            Capacity = 10,
            Status = TrainingStatus.Planned
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(training);

        var act = () => _service.RegisterAsync(99, 1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registration is not open for this training.");
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
            Registrations = new List<Registration>()
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(training);
        _regRepoMock.Setup(r => r.GetByTrainingIdAsync(2)).ReturnsAsync(new List<Registration>());
        _regRepoMock.Setup(r => r.GetByUserAndTrainingAsync(99, 2)).ReturnsAsync((Registration?)null);
        _regRepoMock.Setup(r => r.AddAsync(It.IsAny<Registration>())).Returns(Task.CompletedTask);

        var result = await _service.RegisterAsync(99, 2);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsyncThrowsForNewTrainingBeyond4Days()
    {
        var training = new Training
        {
            Id = 3,
            Title = "New Training Far",
            DateTime = DateTime.UtcNow.AddDays(5),
            Capacity = 10,
            Status = TrainingStatus.New
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(training);

        var act = () => _service.RegisterAsync(99, 3);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registration is not open for this training.");
    }

    [Fact]
    public async Task CancelAsyncThrowsWhenTrainingHasAlreadyStarted()
    {
        var training = new Training
        {
            Id = 4,
            Title = "Past Training",
            DateTime = DateTime.UtcNow.AddHours(-2),
            Capacity = 10,
            Status = TrainingStatus.Planned
        };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(training);

        var act = () => _service.CancelAsync(99, 4);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registration changes are no longer allowed for this training.");
    }
}
