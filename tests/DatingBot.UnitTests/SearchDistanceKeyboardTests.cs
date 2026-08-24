using DatingBot.Bot.Keyboards;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DatingBot.UnitTests;

public class SearchDistanceKeyboardTests
{
    [Theory]
    [InlineData(AppLanguage.Russian)]
    [InlineData(AppLanguage.English)]
    [InlineData(AppLanguage.Ukrainian)]
    public void RegistrationKeyboards_GetSearchDistanceKeyboard_Should_ContainAllFourDistanceOptions(AppLanguage lang)
    {
        // Act
        var keyboard = RegistrationKeyboards.GetSearchDistanceKeyboard(lang);

        // Assert
        keyboard.InlineKeyboard.Should().NotBeNull();
        var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();

        allButtons.Should().Contain(b => b.CallbackData == "reg_distance:1"); // UpTo100Km
        allButtons.Should().Contain(b => b.CallbackData == "reg_distance:2"); // UpTo500Km
        allButtons.Should().Contain(b => b.CallbackData == "reg_distance:3"); // SameCountry
        allButtons.Should().Contain(b => b.CallbackData == "reg_distance:4"); // Anywhere
    }

    [Theory]
    [InlineData(AppLanguage.Russian)]
    [InlineData(AppLanguage.English)]
    public void ProfileKeyboards_GetSearchPreferencesKeyboard_Should_HaveDoneButtonBeforeCancel_And_ContainSearchDistance(AppLanguage lang)
    {
        // Act
        var keyboard = ProfileKeyboards.GetSearchPreferencesKeyboard(AgeCategoryFilter.None, lang);

        // Assert
        var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();

        allButtons.Should().Contain(b => b.CallbackData == "edit:search_distance");

        var doneIndex = allButtons.FindIndex(b => b.CallbackData == "edit_age_cat_save");
        var cancelIndex = allButtons.FindIndex(b => b.CallbackData == "edit:cancel");

        doneIndex.Should().BeGreaterThan(-1);
        cancelIndex.Should().BeGreaterThan(-1);
        doneIndex.Should().BeLessThan(cancelIndex, "Кнопка «Готово» должна быть перед кнопкой «Отмена»");
    }

    [Fact]
    public void ProfileKeyboards_GetEditSearchDistanceKeyboard_Should_DisplayCheckmarkOnSelectedOption()
    {
        // Act
        var keyboard100 = ProfileKeyboards.GetEditSearchDistanceKeyboard(SearchDistancePreference.UpTo100Km, AppLanguage.Russian);
        var keyboardCountry = ProfileKeyboards.GetEditSearchDistanceKeyboard(SearchDistancePreference.SameCountry, AppLanguage.Russian);

        // Assert
        var buttons100 = keyboard100.InlineKeyboard.SelectMany(row => row).ToList();
        var btn100 = buttons100.First(b => b.CallbackData == "edit_distance:1");
        btn100.Text.Should().StartWith("✅");

        var buttonsCountry = keyboardCountry.InlineKeyboard.SelectMany(row => row).ToList();
        var btnCountry = buttonsCountry.First(b => b.CallbackData == "edit_distance:3");
        btnCountry.Text.Should().StartWith("✅");

        buttons100.Should().Contain(b => b.CallbackData == "edit:search_params");
    }
}
