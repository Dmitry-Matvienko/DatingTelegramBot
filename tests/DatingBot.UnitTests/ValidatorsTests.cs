using DatingBot.Application.Validators;
using FluentAssertions;
using Xunit;

namespace DatingBot.UnitTests;

public class ValidatorsTests
{
    private readonly NameValidator _nameValidator = new();
    private readonly AgeValidator _ageValidator = new();
    private readonly HeightValidator _heightValidator = new();
    private readonly CityValidator _cityValidator = new();
    private readonly AiDescriptionValidator _aiDescriptionValidator = new();

    [Theory]
    [InlineData("Алексей")]
    [InlineData("Анна-Мария")]
    [InlineData("John Doe")]
    public void Should_PassValidation_When_NameIsValid(string name)
    {
        var result = _nameValidator.Validate(name);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Alex123!")]
    public void Should_FailValidation_When_NameIsInvalid(string name)
    {
        var result = _nameValidator.Validate(name);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(14)]
    [InlineData(18)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public void Should_PassValidation_When_AgeIsWithinRange(int age)
    {
        var result = _ageValidator.Validate(age);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(9)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public void Should_FailValidation_When_AgeIsOutOfRange(int age)
    {
        var result = _ageValidator.Validate(age);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(150)]
    [InlineData(180)]
    [InlineData(210)]
    public void Should_PassValidation_When_HeightIsValid(int height)
    {
        var result = _heightValidator.Validate(height);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(80)]
    [InlineData(260)]
    public void Should_FailValidation_When_HeightIsOutOfRange(int height)
    {
        var result = _heightValidator.Validate(height);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Москва")]
    [InlineData("Санкт-Петербург")]
    [InlineData("Ростов-на-Дону")]
    [InlineData("New York")]
    public void Should_PassValidation_When_CityIsValid(string city)
    {
        var result = _cityValidator.Validate(city);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("M")]
    [InlineData("Москва123")]
    [InlineData("12345")]
    [InlineData("City!")]
    public void Should_FailValidation_When_CityIsInvalid(string city)
    {
        var result = _cityValidator.Validate(city);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_PassValidation_When_AiDescriptionIsValid()
    {
        var result = _aiDescriptionValidator.Validate("Люблю активный отдых, программирование и путешествия.");
        result.IsValid.Should().BeTrue();
    }
}
