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
    private readonly Mock<IDateTimeFormatService> _dateTimeFormatServiceMock = new();
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
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);
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
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);
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
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);
        var dto = new CreateTrainingDto { Title = "Pilates", LocationId = 2, DateTime = DateTime.Now.AddDays(1), Capacity = 15, TrainerId = 1, GroupId = 5 };
        var result = await service.CreateAsync(dto);
        result.Should().NotBeNull();
        result.Title.Should().Be("Pilates");
        _trainingRepoMock.Verify(r => r.AddAsync(It.IsAny<Training>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncWithTrainerStartsInPlanning()
    {
        using var ctx = CreateInMemoryContext();
        _trainingRepoMock.Setup(r => r.AddAsync(It.IsAny<Training>())).Returns(Task.CompletedTask);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);
        var dto = new CreateTrainingDto { Title = "Pilates", DateTime = DateTime.Now.AddDays(1), Capacity = 15, TrainerId = 1, GroupId = 5 };

        var result = await service.CreateAsync(dto);

        result.Status.Should().Be(TrainingStatus.InPlanning);
    }

    [Fact]
    public async Task CreateAsyncWithoutTrainerStartsAsNew()
    {
        using var ctx = CreateInMemoryContext();
        _trainingRepoMock.Setup(r => r.AddAsync(It.IsAny<Training>())).Returns(Task.CompletedTask);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);
        var dto = new CreateTrainingDto { Title = "Pilates", DateTime = DateTime.Now.AddDays(1), Capacity = 15, TrainerId = null, GroupId = 5 };

        var result = await service.CreateAsync(dto);

        result.Status.Should().Be(TrainingStatus.New);
        result.TrainerId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsyncGeneratesAutoTitleWhenLeftBlank()
    {
        using var ctx = CreateInMemoryContext();
        var country = new Country { Id = 1, Code = "CH", Name = "Switzerland" };
        ctx.Countries.Add(country);
        ctx.Groups.Add(new Group { Id = 5, Name = "Group A", CountryId = 1, Country = country });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        _dateTimeFormatServiceMock.Setup(s => s.GetCultureForCountry("CH")).Returns(System.Globalization.CultureInfo.InvariantCulture);
        _trainingRepoMock.Setup(r => r.AddAsync(It.IsAny<Training>())).Returns(Task.CompletedTask);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);
        var trainingDate = new DateTime(2026, 9, 10);
        var dto = new CreateTrainingDto { Title = "", DateTime = trainingDate, Capacity = 15, GroupId = 5 };

        var result = await service.CreateAsync(dto);

        result.Title.Should().Be($"Training of {trainingDate.ToString("d", System.Globalization.CultureInfo.InvariantCulture)}");
    }

    [Fact]
    public async Task TakeAsyncAssignsTrainerAndMovesToInPlanning()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 30, Title = "Unassigned", DateTime = DateTime.UtcNow.AddDays(1), Capacity = 10, TrainerId = null, Status = TrainingStatus.New };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(training);
        _trainingRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Training>())).Returns(Task.CompletedTask);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var result = await service.TakeAsync(30, trainerId: 7, TestContext.Current.CancellationToken);

        result.TrainerId.Should().Be(7);
        result.Status.Should().Be(TrainingStatus.InPlanning);
    }

    [Fact]
    public async Task TakeAsyncThrowsWhenAlreadyAssigned()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 31, Title = "Assigned", DateTime = DateTime.UtcNow.AddDays(1), Capacity = 10, TrainerId = 5, Status = TrainingStatus.InPlanning };
        _trainingRepoMock.Setup(r => r.GetByIdAsync(31)).ReturnsAsync(training);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var act = () => service.TakeAsync(31, trainerId: 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This training already has a trainer assigned.");
    }

    [Fact]
    public async Task ReleaseTrainerAsyncClearsTrainerAndRevertsToNew()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 32, Title = "Mine", DateTime = DateTime.UtcNow.AddDays(1), Capacity = 10, TrainerId = 7, Status = TrainingStatus.InPlanning };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        await service.ReleaseTrainerAsync(32, requestingUserId: 7, TestContext.Current.CancellationToken);

        var updated = await ctx.Trainings.FindAsync([32], TestContext.Current.CancellationToken);
        updated!.TrainerId.Should().BeNull();
        updated.Status.Should().Be(TrainingStatus.New);
    }

    [Fact]
    public async Task ReleaseTrainerAsyncThrowsWhenNotAssignedTrainer()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 33, Title = "Mine", DateTime = DateTime.UtcNow.AddDays(1), Capacity = 10, TrainerId = 7, Status = TrainingStatus.InPlanning };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var act = () => service.ReleaseTrainerAsync(33, requestingUserId: 999);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only the assigned trainer can release this training.");
    }

    [Fact]
    public async Task ReleaseTrainerAsyncThrowsWhenInProgress()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 34, Title = "Running", DateTime = DateTime.UtcNow, Capacity = 10, TrainerId = 7, Status = TrainingStatus.InProgress };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var act = () => service.ReleaseTrainerAsync(34, requestingUserId: 7);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A training that is in progress or already done cannot be released.");
    }

    [Fact]
    public async Task ReassignTrainerAsyncSetsTrainerAndMovesNewToInPlanning()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 35, Title = "Unassigned", DateTime = DateTime.UtcNow.AddDays(1), Capacity = 10, TrainerId = null, Status = TrainingStatus.New };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        await service.ReassignTrainerAsync(35, newTrainerId: 9, TestContext.Current.CancellationToken);

        var updated = await ctx.Trainings.FindAsync([35], TestContext.Current.CancellationToken);
        updated!.TrainerId.Should().Be(9);
        updated.Status.Should().Be(TrainingStatus.InPlanning);
    }

    [Fact]
    public async Task ReassignTrainerAsyncClearingTrainerRevertsToNew()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 36, Title = "Planned", DateTime = DateTime.UtcNow.AddDays(1), Capacity = 10, TrainerId = 9, Status = TrainingStatus.Planned };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        await service.ReassignTrainerAsync(36, newTrainerId: null, TestContext.Current.CancellationToken);

        var updated = await ctx.Trainings.FindAsync([36], TestContext.Current.CancellationToken);
        updated!.TrainerId.Should().BeNull();
        updated.Status.Should().Be(TrainingStatus.New);
    }

    [Fact]
    public async Task ReassignTrainerAsyncThrowsWhenDone()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 37, Title = "Done", DateTime = DateTime.UtcNow.AddDays(-1), Capacity = 10, TrainerId = 9, Status = TrainingStatus.Done };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var act = () => service.ReassignTrainerAsync(37, newTrainerId: 1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The trainer cannot be changed once the training has started.");
    }

    [Fact]
    public async Task ConfirmPlannedAsyncMovesInPlanningToPlanned()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 38, Title = "Planning", DateTime = DateTime.UtcNow.AddDays(1), Capacity = 10, TrainerId = 7, Status = TrainingStatus.InPlanning };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        await service.ConfirmPlannedAsync(38, TestContext.Current.CancellationToken);

        var updated = await ctx.Trainings.FindAsync([38], TestContext.Current.CancellationToken);
        updated!.Status.Should().Be(TrainingStatus.Planned);
    }

    [Fact]
    public async Task ConfirmPlannedAsyncThrowsWhenNoTrainerAssigned()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 39, Title = "Unassigned", DateTime = DateTime.UtcNow.AddDays(1), Capacity = 10, TrainerId = null, Status = TrainingStatus.New };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var act = () => service.ConfirmPlannedAsync(39);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A trainer must be assigned before the training can be confirmed as planned.");
    }

    [Fact]
    public async Task StartAsyncMovesPlannedToInProgressForAssignedTrainer()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 40, Title = "Planned", DateTime = DateTime.UtcNow, Capacity = 10, TrainerId = 7, Status = TrainingStatus.Planned };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        await service.StartAsync(40, requestingTrainerId: 7, TestContext.Current.CancellationToken);

        var updated = await ctx.Trainings.FindAsync([40], TestContext.Current.CancellationToken);
        updated!.Status.Should().Be(TrainingStatus.InProgress);
    }

    [Fact]
    public async Task StartAsyncThrowsWhenRequestedByNonAssignedTrainer()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 41, Title = "Planned", DateTime = DateTime.UtcNow, Capacity = 10, TrainerId = 7, Status = TrainingStatus.Planned };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var act = () => service.StartAsync(41, requestingTrainerId: 999);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only the assigned trainer can start this training.");
    }

    [Fact]
    public async Task LockAttendanceAsyncMovesTrainingToDone()
    {
        using var ctx = CreateInMemoryContext();
        var training = new Training { Id = 42, Title = "Running", DateTime = DateTime.UtcNow.AddHours(-1), Capacity = 10, TrainerId = 7, Status = TrainingStatus.InProgress };
        ctx.Trainings.Add(training);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        await service.LockAttendanceAsync(42, TestContext.Current.CancellationToken);

        var updated = await ctx.Trainings.FindAsync([42], TestContext.Current.CancellationToken);
        updated!.Status.Should().Be(TrainingStatus.Done);
        updated.AttendanceLocked.Should().BeTrue();
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
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

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
            Blocks =
            [
                new()
                {
                    Id = 5,
                    TrainingId = 11,
                    OrderIndex = 2,
                    Title = "Block B",
                    PlannedDurationMinutes = 20,
                    TrainingBlockTags =
                    [
                        new() { TrainingBlockId = 5, TagId = 999, Tag = null! },
                        new() { TrainingBlockId = 5, TagId = 2, Tag = validTag }
                    ]
                },
                new()
                {
                    Id = 4,
                    TrainingId = 11,
                    OrderIndex = 1,
                    Title = "Block A",
                    PlannedDurationMinutes = 10
                }
            ]
        };

        _trainingRepoMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(training);
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

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

        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);
        await service.SetStatusAsync(20, TrainingStatus.Planned, TestContext.Current.CancellationToken);

        var updated = await ctx.Trainings.FindAsync([20], TestContext.Current.CancellationToken);
        updated!.Status.Should().Be(TrainingStatus.Planned);
    }

    [Fact]
    public async Task GetNextAvailableDateForGroupAsyncReturnsNextWeekdayWhenNoConflict()
    {
        using var ctx = CreateInMemoryContext();
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var weekday = DayOfWeek.Monday;
        var result = await service.GetNextAvailableDateForGroupAsync(groupId: 99, weekday, TestContext.Current.CancellationToken);

        result.DayOfWeek.Should().Be(weekday);
        result.Date.Should().BeAfter(DateTime.Today);
    }

    [Fact]
    public async Task GetNextAvailableDateForGroupAsyncSkipsOneWeekWhenFirstDateOccupied()
    {
        using var ctx = CreateInMemoryContext();
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var weekday = DayOfWeek.Wednesday;

        int offset = ((int)weekday - (int)DateTime.Today.DayOfWeek + 7) % 7;
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
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var weekday = DayOfWeek.Friday;

        int offset = ((int)weekday - (int)DateTime.Today.DayOfWeek + 7) % 7;
        if (offset == 0) offset = 7;
        var firstOccurrence = DateTime.Today.AddDays(offset);

        for (int week = 0; week < 3; week++)
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
        var service = new TrainingService(_trainingRepoMock.Object, ctx, _runtimeModeServiceMock.Object, _dateTimeFormatServiceMock.Object);

        var weekday = DayOfWeek.Tuesday;

        int offset = ((int)weekday - (int)DateTime.Today.DayOfWeek + 7) % 7;
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
