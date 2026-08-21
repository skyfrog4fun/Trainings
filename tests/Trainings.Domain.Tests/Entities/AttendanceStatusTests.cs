using FluentAssertions;
using Trainings.Domain.Enums;
using Xunit;

namespace Trainings.Domain.Tests.Entities;

public class AttendanceStatusTests
{
    [Fact]
    public void AttendanceStatusHasPresentValue() => ((int)AttendanceStatus.Present).Should().Be(0);

    [Fact]
    public void AttendanceStatusHasAbsentValue() => ((int)AttendanceStatus.Absent).Should().Be(1);

    [Fact]
    public void AttendanceStatusHasPartiallyPresentValue() => ((int)AttendanceStatus.PartiallyPresent).Should().Be(2);

    [Fact]
    public void AttendanceStatusHasExactlyThreeValues()
    {
        var values = Enum.GetValues<AttendanceStatus>();
        values.Should().HaveCount(3);
    }
}
