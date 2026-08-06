using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Trainings.Application.DTOs;
using Trainings.Application.Interfaces;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Domain.Interfaces;
using Trainings.Infrastructure.Data;
using Trainings.Infrastructure.Services;
using Xunit;

namespace Trainings.Application.Tests.Services;

public class TrainingServiceTests
{
    private readonly Mock<ITrainingRepository> _trainingRepoMock = new();
    private readonly Mock<IAppRuntimeModeService> _runtimeModeServiceMock = new();
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        var ctx = new ApplicationDbContext(options);
        ctx.Database.OpenConnection();
        ctx.Database.EnsureCreated();
        // Disable FK enforcement: these tests verify service logic, not relational integrity.
        ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        return ctx;
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullWhenNotFound()
    {
        using var ctx = CreateInMemoryContext();
        _trainingRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Training?)null);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object);
        var result = await service.GetByIdAsync(99);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsyncReturnsDtoWhenFound()
    {
        using var ctx = CreateInMemoryContext();
        var location = new Location { Id = 1, Name = "Studio", CityName = "Zurich", IsActive = true };
        var training = new Training { Id = 1, Title = "Yoga", LocationId = 1, Location = location, DateTime = DateTime.Now, DurationMinutes = 75, Capacity = 10 };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(training);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object);
        var result = await service.GetByIdAsync(1);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Yoga");
        result.DurationMinutes.Should().Be(75);
    }

    [Fact]
    public async Task CreateAsyncAddsTraining()
    {
        using var ctx = CreateInMemoryContext();
        _trainingRepoMock.Setup(r => r.AddAsync(It.IsAny<Training>())).Returns(Task.CompletedTask);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object);
        var dto = new CreateTrainingDto { Title = "Pilates", LocationId = 2, DateTime = DateTime.Now.AddDays(1), Capacity = 15, TrainerId = 1, GroupId = 5 };
        var result = await service.CreateAsync(dto);
        result.Should().NotBeNull();
        result.Title.Should().Be("Pilates");
        _trainingRepoMock.Verify(r => r.AddAsync(It.IsAny<Training>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsyncHandlesLegacyMissingRelationsWithoutThrowing()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training
        {
            Id = 10,
            Title = "Legacy Training",
            DateTime = DateTime.UtcNow,
            Capacity = 12,
            TrainerId = 999,
            GroupId = 100,
            Group = new Group { Id = 100, Name = "Legacy Group" },
            Trainer = null!,
            Location = null
        };

        _trainingRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(training);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object);

        var result = await service.GetByIdAsync(10);

        result.Should().NotBeNull();
        result!.TrainerName.Should().BeEmpty();
        result.GroupName.Should().Be("Legacy Group");
        result.GroupCountry.Should().BeNull();
        result.LocationName.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsyncSkipsDanglingBlockTags()
    {
        using var ctx = CreateInMemoryContext();
        var validTag = new Tag { Id = 2, Name = "Technique" };

        var training = new Training
        {
            Id = 11,
            Title = "Legacy With Blocks",
            DateTime = DateTime.UtcNow,
            Capacity = 10,
            TrainerId = 1,
            Blocks = new List<TrainingBlock>
            {
                new()
                {
                    Id = 5,
                    TrainingId = 11,
                    OrderIndex = 2,
                    Title = "Block B",
                    PlannedDurationMinutes = 20,
                    TrainingBlockTags = new List<TrainingBlockTag>
                    {
                        new() { TrainingBlockId = 5, TagId = 999, Tag = null! },
                        new() { TrainingBlockId = 5, TagId = 2, Tag = validTag }
                    }
                },
                new()
                {
                    Id = 4,
                    TrainingId = 11,
                    OrderIndex = 1,
                    Title = "Block A",
                    PlannedDurationMinutes = 10
                }
            }
        };

        _trainingRepoMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(training);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object);

        var result = await service.GetByIdAsync(11);

        result.Should().NotBeNull();
        result!.Blocks.Should().HaveCount(2);
        result.Blocks.Select(b => b.OrderIndex).Should().ContainInOrder(1, 2);
        result.Blocks[1].Tags.Should().ContainSingle(t => t.Id == 2 && t.Name == "Technique");
    }

    [Fact]
    public async Task SetStatusAsyncChangesTrainingStatus()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training
        {
            Id = 20,
            Title = "Test",
            DateTime = DateTime.UtcNow.AddDays(1),
            Capacity = 10,
            TrainerId = 1,
            GroupId = 1
        };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object);
        await service.SetStatusAsync(20, TrainingStatus.Planned, TestContext.Current.CancellationToken);

        var updated = await ctx.Trainings.FindAsync([20], TestContext.Current.CancellationToken);
        updated!.Status.Should().Be(TrainingStatus.Planned);
    }

    [Fact]
    public async Task GetNextAvailableDateForGroupAsyncReturnsNextWeekdayWhenNoConflict()
    {
        using var ctx = CreateInMemoryContext();
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object);

        var weekday = DayOfWeek.Monday;
        var result = await service.GetNextAvailableDateForGroupAsync(groupId: 99, weekday, TestContext.Current.CancellationToken);

        result.DayOfWeek.Should().Be(weekday);
        result.Date.Should().BeAfter(DateTime.Today);
    }

    [Fact]
    public async Task GetNextAvailableDateForGroupAsyncSkipsOneWeekWhenFirstDateOccupied()
    {
        using var ctx = CreateInMemoryContext();
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object);

        var weekday = DayOfWeek.Wednesday;

        var offset = ((int)weekday - (int)DateTime.Today.DayOfWeek + 7) % 7;
        if (offset == 0) offset = 7;
        var firstOccurrence = DateTime.Today.AddDays(offset);

        ctx.Trainings.Add(new Training
        {
            Title = "Existing",
            DateTime = firstOccurrence.AddHours(19),
            Capacity = 10,
            TrainerId = 1,
            GroupId = 42
        });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await service.GetNextAvailableDateForGroupAsync(groupId: 42, weekday, TestContext.Current.CancellationToken);

        result.Date.Should().Be(firstOccurrence.AddDays(7).Date);
        result.DayOfWeek.Should().Be(weekday);
    }

    [Fact]
    public async Task GetNextAvailableDateForGroupAsyncSkipsMultipleWeeksWhenSeveralDatesOccupied()
    {
        using var ctx = CreateInMemoryContext();
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object);

        var weekday = DayOfWeek.Friday;

        var offset = ((int)weekday - (int)DateTime.Today.DayOfWeek + 7) % 7;
        if (offset == 0) offset = 7;
        var firstOccurrence = DateTime.Today.AddDays(offset);

        for (var week = 0; week < 3; week++)
        {
            ctx.Trainings.Add(new Training
            {
                Title = $"Existing week {week}",
                DateTime = firstOccurrence.AddDays(week * 7).AddHours(18),
                Capacity = 10,
                TrainerId = 1,
                GroupId = 7
            });
        }

        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await service.GetNextAvailableDateForGroupAsync(groupId: 7, weekday, TestContext.Current.CancellationToken);

        result.Date.Should().Be(firstOccurrence.AddDays(3 * 7).Date);
        result.DayOfWeek.Should().Be(weekday);
    }

    [Fact]
    public async Task GetNextAvailableDateForGroupAsyncIgnoresOtherGroups()
    {
        using var ctx = CreateInMemoryContext();
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object);

        var weekday = DayOfWeek.Tuesday;

        var offset = ((int)weekday - (int)DateTime.Today.DayOfWeek + 7) % 7;
        if (offset == 0) offset = 7;
        var firstOccurrence = DateTime.Today.AddDays(offset);

        // Another group has a training on the same date — should not block group 5
        ctx.Trainings.Add(new Training
        {
            Title = "Other group training",
            DateTime = firstOccurrence.AddHours(20),
            Capacity = 10,
            TrainerId = 1,
            GroupId = 99
        });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await service.GetNextAvailableDateForGroupAsync(groupId: 5, weekday, TestContext.Current.CancellationToken);

        result.Date.Should().Be(firstOccurrence.Date);
    }
}
