using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Trainings.Application.DTOs;
using Trainings.Domain.Entities;
using Trainings.Domain.Interfaces;
using Trainings.Infrastructure.Data;
using Trainings.Infrastructure.Services;
using Xunit;

namespace Trainings.Application.Tests.Services;

public class TrainingServiceTests
{
    private readonly Mock<ITrainingRepository> _trainingRepoMock = new();
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        var ctx = new ApplicationDbContext(options);
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullWhenNotFound()
    {
        using var ctx = CreateInMemoryContext();
        _trainingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Training?)null);
        var service = new TrainingService(_trainingRepoMock.Object, ctx);
        var result = await service.GetByIdAsync(99);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsyncReturnsDtoWhenFound()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 1, Title = "Yoga", Location = "Studio", DateTime = DateTime.Now, Capacity = 10 };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(training);
        var service = new TrainingService(_trainingRepoMock.Object, ctx);
        var result = await service.GetByIdAsync(1);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Yoga");
    }

    [Fact]
    public async Task CreateAsyncAddsTraining()
    {
        using var ctx = CreateInMemoryContext();
        _trainingRepoMock.Setup(r => r.AddAsync(It.IsAny<Training>())).Returns(Task.CompletedTask);
        var service = new TrainingService(_trainingRepoMock.Object, ctx);
        var dto = new CreateTrainingDto { Title = "Pilates", Location = "Gym", DateTime = DateTime.Now.AddDays(1), Capacity = 15, TrainerId = 1 };
        var result = await service.CreateAsync(dto);
        result.Should().NotBeNull();
        result.Title.Should().Be("Pilates");
        _trainingRepoMock.Verify(r => r.AddAsync(It.IsAny<Training>()), Times.Once);
    }
}
