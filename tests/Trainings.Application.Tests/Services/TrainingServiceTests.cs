using FluentAssertions;
using Moq;
using Trainings.Application.DTOs;
using Trainings.Application.Services;
using Trainings.Domain.Entities;
using Trainings.Domain.Interfaces;
using Xunit;

namespace Trainings.Application.Tests.Services;

public class TrainingServiceTests
{
    private readonly Mock<ITrainingRepository> _trainingRepoMock = new();
    private readonly TrainingService _service;

    public TrainingServiceTests()
    {
        _service = new TrainingService(_trainingRepoMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _trainingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Training?)null);
        var result = await _service.GetByIdAsync(99);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        var training = new Training { Id = 1, Title = "Yoga", Location = "Studio", DateTime = DateTime.Now, Capacity = 10 };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(training);
        var result = await _service.GetByIdAsync(1);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Yoga");
    }

    [Fact]
    public async Task CreateAsync_AddsTraining()
    {
        _trainingRepoMock.Setup(r => r.AddAsync(It.IsAny<Training>())).Returns(Task.CompletedTask);
        var dto = new CreateTrainingDto { Title = "Pilates", Location = "Gym", DateTime = DateTime.Now.AddDays(1), Capacity = 15, TrainerId = 1 };
        var result = await _service.CreateAsync(dto);
        result.Should().NotBeNull();
        result.Title.Should().Be("Pilates");
        _trainingRepoMock.Verify(r => r.AddAsync(It.IsAny<Training>()), Times.Once);
    }
}
