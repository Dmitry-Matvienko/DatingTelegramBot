using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DatingBot.UnitTests.Services;

public class SearchTipsTests
{
    private readonly LocalizationService _loc = new();

    [Theory]
    [InlineData(AppLanguage.Russian)]
    [InlineData(AppLanguage.Ukrainian)]
    [InlineData(AppLanguage.English)]
    [InlineData(AppLanguage.Hindi)]
    [InlineData(AppLanguage.Portuguese)]
    [InlineData(AppLanguage.Indonesian)]
    public void GetRandomSearchTip_ShouldReturnNonEmptyString_ForEveryLanguage(AppLanguage language)
    {
        // Act
        var tip = _loc.GetRandomSearchTip(language);

        // Assert
        tip.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(AppLanguage.Russian)]
    [InlineData(AppLanguage.Ukrainian)]
    [InlineData(AppLanguage.English)]
    [InlineData(AppLanguage.Hindi)]
    [InlineData(AppLanguage.Portuguese)]
    [InlineData(AppLanguage.Indonesian)]
    public void AllSearchTipKeys_ShouldHaveTranslationsInAllLanguages(AppLanguage language)
    {
        // Arrange
        var tipKeys = _loc.GetAllSearchTipKeys();

        // Assert
        tipKeys.Should().HaveCount(15);
        foreach (var key in tipKeys)
        {
            var translation = _loc.Get(language, key);
            translation.Should().NotBeNullOrWhiteSpace($"key '{key}' must have translation for {language}");
            translation.Should().NotBe(key, $"key '{key}' should not fall back to key name in {language}");
        }
    }

    [Fact]
    public void GetRandomSearchTip_Russian_ShouldContainExpectedChannelsAndContacts()
    {
        // Arrange
        var tip2 = _loc.Get(AppLanguage.Russian, "Search_Tip_2");
        var tip6 = _loc.Get(AppLanguage.Russian, "Search_Tip_6");
        var tip14 = _loc.Get(AppLanguage.Russian, "Search_Tip_14");

        // Assert
        tip2.Should().Contain("@TheBestDating");
        tip6.Should().Contain("@KimeLowe65");
        tip14.Should().Contain("фейки, оскорбления в анкете и запрещенный контент");
    }

    [Fact]
    public void GetRandomSearchTip_ShouldReturnKnownTip_FromDictionary()
    {
        // Act
        var tip = _loc.GetRandomSearchTip(AppLanguage.Russian);
        var allRussianTips = _loc.GetAllSearchTipKeys().Select(k => _loc.Get(AppLanguage.Russian, k)).ToList();

        // Assert
        allRussianTips.Should().Contain(tip);
    }
}
