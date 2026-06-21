using FluentAssertions;
using Trainings.Domain.Entities;
using Trainings.Domain.Enums;
using Xunit;

namespace Trainings.Domain.Tests.Entities;

public class TrainingTests
{
    [Fact]
    public void TrainingDefaultStatusIsNew()
    {
        var training = new Training();
        training.Status.Should().Be(TrainingStatus.New);
    }

    [Fact]
    public void TrainingCanSetCapacity()
    {
        var training = new Training { Capacity = 20 };
        training.Capacity.Should().Be(20);
    }

    [Fact]
    public void TrainingHasEmptyRegistrationsCollection()
    {
        var training = new Training();
        training.Registrations.Should().NotBeNull();
        training.Registrations.Should().BeEmpty();
    }

    [Fact]
    public void TrainingAttendanceLockedDefaultIsFalse()
    {
        var training = new Training();
        training.AttendanceLocked.Should().BeFalse();
    }

    [Fact]
    public void TrainingAttendanceLockedAtDefaultIsNull()
    {
        var training = new Training();
        training.AttendanceLockedAt.Should().BeNull();
    }

    [Fact]
    public void TrainingCanSetAttendanceLocked()
    {
        var training = new Training { AttendanceLocked = true, AttendanceLockedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc) };
        training.AttendanceLocked.Should().BeTrue();
        training.AttendanceLockedAt.Should().Be(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    }
}
