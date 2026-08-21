using FluentAssertions;
using Moq;
using Trainings.Application.Interfaces;
using Trainings.Application.Services;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Trainings.Domain.Interfaces;
using Xunit;

namespace Trainings.Application.Tests.Services;

public class AttendanceServiceTests
{
    private readonly Mock<IAttendanceRepository> _repoMock = new();
    private readonly Mock<IAppRuntimeModeService> _modeMock = new();

    private AttendanceService CreateService() => new(_repoMock.Object, _modeMock.Object);

    [Fact]
    public async Task BulkSaveAsyncCallsRecordAttendanceForEachEntry()
    {
        _repoMock.Setup(r => r.GetByUserAndTrainingAsync(It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync((Attendance?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);

        var service = CreateService();
        var map = new Dictionary<int, AttendanceStatus>
        {
            { 1, AttendanceStatus.Present },
            { 2, AttendanceStatus.Absent },
            { 3, AttendanceStatus.PartiallyPresent }
        };

        await service.BulkSaveAsync(trainingId: 10, map, savedByTrainerId: 99, TestContext.Current.CancellationToken);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Attendance>()), Times.Exactly(3));
    }

    [Fact]
    public async Task BulkSaveAsyncUpdatesExistingAttendanceRecord()
    {
        var existing = new Attendance
        {
            Id = 5,
            UserId = 1,
            TrainingId = 10,
            Status = AttendanceStatus.Absent,
            RecordedAt = DateTime.UtcNow.AddDays(-1),
            RecordedByTrainerId = 7
        };
        _repoMock.Setup(r => r.GetByUserAndTrainingAsync(1, 10)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Attendance>())).Returns(Task.CompletedTask);

        var service = CreateService();
        var map = new Dictionary<int, AttendanceStatus> { { 1, AttendanceStatus.Present } };

        await service.BulkSaveAsync(trainingId: 10, map, savedByTrainerId: 99, TestContext.Current.CancellationToken);

        _repoMock.Verify(r => r.UpdateAsync(It.Is<Attendance>(a => a.Status == AttendanceStatus.Present)), Times.Once);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Attendance>()), Times.Never);
    }

    [Fact]
    public async Task BulkSaveAsyncWithEmptyMapDoesNothing()
    {
        var service = CreateService();
        var map = new Dictionary<int, AttendanceStatus>();

        await service.BulkSaveAsync(trainingId: 10, map, savedByTrainerId: 99, TestContext.Current.CancellationToken);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Attendance>()), Times.Never);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Attendance>()), Times.Never);
    }

    [Fact]
    public async Task GetByTrainingIdAsyncReturnsEmpty()
    {
        _repoMock.Setup(r => r.GetByTrainingIdAsync(42)).ReturnsAsync([]);
        var service = CreateService();

        var result = await service.GetByTrainingIdAsync(42);

        result.Should().BeEmpty();
    }
}
