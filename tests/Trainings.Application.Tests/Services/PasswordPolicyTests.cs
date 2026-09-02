using FluentAssertions;
using Trainings.Application.Exceptions;
using Trainings.Application.Services;
using Xunit;

namespace Trainings.Application.Tests.Services;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Short1!")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecialChar1")]
    public void ValidateReturnsFalseForWeakPasswords(string password)
    {
        bool result = PasswordPolicy.Validate(password, out var error);

        result.Should().BeFalse();
        error.Should().NotBe(PasswordValidationError.None);
    }

    [Theory]
    [InlineData("Password1!")]
    [InlineData("C0mplex-Pass")]
    public void ValidateReturnsTrueForStrongPasswords(string password)
    {
        bool result = PasswordPolicy.Validate(password, out var error);

        result.Should().BeTrue();
        error.Should().Be(PasswordValidationError.None);
    }

    [Fact]
    public void GenerateProducesPasswordThatPassesValidate()
    {
        string generated = PasswordPolicy.Generate();

        bool result = PasswordPolicy.Validate(generated, out var error);

        result.Should().BeTrue();
        error.Should().Be(PasswordValidationError.None);
    }

    [Fact]
    public void EnsureValidThrowsArgumentExceptionForWeakPassword()
    {
        var act = () => PasswordPolicy.EnsureValid("weak", "password");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("password");
    }

    [Fact]
    public void EnsureValidThrowsPasswordPolicyViolationExceptionWithMatchingErrorReason()
    {
        var act = () => PasswordPolicy.EnsureValid("weak", "password");

        act.Should().Throw<PasswordPolicyViolationException>()
            .Which.Error.Should().Be(PasswordValidationError.TooShort);
    }

    [Fact]
    public void EnsureValidDoesNotThrowForStrongPassword()
    {
        var act = () => PasswordPolicy.EnsureValid("Password1!", "password");

        act.Should().NotThrow();
    }
}
