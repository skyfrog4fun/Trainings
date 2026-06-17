using FluentAssertions;
using Xunit;

namespace Trainings.Domain.Tests.Entities;

/// <summary>
/// Verifies the remaining-time / overbooked-time formula used on the training planning page.
/// The formula is: remaining = trainingDuration - sum(blockDurations).
/// Positive → remaining; negative → overbooked.
/// </summary>
public class RemainingTimeCalculationTests
{
    private static int CalculateRemaining(int trainingDurationMinutes, IEnumerable<int> blockDurations)
        => trainingDurationMinutes - blockDurations.Sum();

    [Fact]
    public void RemainingIsPositiveWhenBlocksDontFillTraining()
    {
        var remaining = CalculateRemaining(90, [20, 30]);
        remaining.Should().Be(40);
    }

    [Fact]
    public void RemainingIsZeroWhenBlocksExactlyFillTraining()
    {
        var remaining = CalculateRemaining(60, [30, 20, 10]);
        remaining.Should().Be(0);
    }

    [Fact]
    public void RemainingIsNegativeWhenBlocksExceedTraining()
    {
        var remaining = CalculateRemaining(60, [30, 20, 20]);
        remaining.Should().Be(-10);
    }

    [Fact]
    public void RemainingEqualsFullDurationWhenNoBlocks()
    {
        var remaining = CalculateRemaining(90, []);
        remaining.Should().Be(90);
    }
}
