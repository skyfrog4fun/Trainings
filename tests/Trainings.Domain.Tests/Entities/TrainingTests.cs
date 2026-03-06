using FluentAssertions;
using Trainings.Domain.Entities;
using Xunit;

namespace Trainings.Domain.Tests.Entities;

public class TrainingTests
{
    [Fact]
    public void Training_DefaultIsActive_IsTrue()
    {
        var training = new Training();
        training.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Training_CanSetCapacity()
    {
        var training = new Training { Capacity = 20 };
        training.Capacity.Should().Be(20);
    }

    [Fact]
    public void Training_HasEmptyRegistrationsCollection()
    {
        var training = new Training();
        training.Registrations.Should().NotBeNull();
        training.Registrations.Should().BeEmpty();
    }
}
