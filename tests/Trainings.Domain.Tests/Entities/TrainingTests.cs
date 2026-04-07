using FluentAssertions;
using Trainings.Domain.Entities;
using Xunit;

namespace Trainings.Domain.Tests.Entities;

public class TrainingTests
{
    [Fact]
    public void TrainingDefaultIsActiveIsTrue()
    {
        var training = new Training();
        training.IsActive.Should().BeTrue();
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
}
