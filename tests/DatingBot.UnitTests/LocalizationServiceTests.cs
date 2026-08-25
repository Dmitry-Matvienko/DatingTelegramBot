using DatingBot.Application.Services;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DatingBot.UnitTests;

public class LocalizationServiceTests
{
    private readonly LocalizationService _loc = new();

    [Theory]
    [InlineData(AppLanguage.Russian)]
    [InlineData(AppLanguage.Ukrainian)]
    [InlineData(AppLanguage.English)]
    [InlineData(AppLanguage.Hindi)]
    [InlineData(AppLanguage.Portuguese)]
    [InlineData(AppLanguage.Indonesian)]
    public void Get_ShouldReturnNonEmptyStringForAllSupportedLanguages(AppLanguage lang)
    {
        var prompt = _loc.Get(lang, "LanguagePrompt");
        var welcome = _loc.Get(lang, "WelcomeTitle");
        var photo = _loc.Get(lang, "PhotoPrompt");
        var friends = _loc.Get(lang, "Target_Friends");
        var aiBadge = _loc.Get(lang, "Badge_Ai");

        prompt.Should().NotBeNullOrWhiteSpace();
        welcome.Should().NotBeNullOrWhiteSpace();
        photo.Should().NotBeNullOrWhiteSpace();
        friends.Should().NotBeNullOrWhiteSpace();
        aiBadge.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(AppLanguage.Russian, Gender.Male, "Парень 👦")]
    [InlineData(AppLanguage.Ukrainian, Gender.Male, "Хлопець 👦")]
    [InlineData(AppLanguage.English, Gender.Male, "Guy 👦")]
    [InlineData(AppLanguage.Hindi, Gender.Male, "लड़का 👦")]
    [InlineData(AppLanguage.Portuguese, Gender.Male, "Rapaz 👦")]
    [InlineData(AppLanguage.Indonesian, Gender.Male, "Pria 👦")]
    public void GetGenderText_ShouldReturnCorrectLocalizedText(AppLanguage lang, Gender gender, string expected)
    {
        var result = _loc.GetGenderText(lang, gender);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "Btn_ShowWhoRated")]
    [InlineData(AppLanguage.Ukrainian, "Btn_ShowWhoRated")]
    [InlineData(AppLanguage.English, "Btn_ShowWhoRated")]
    [InlineData(AppLanguage.Hindi, "Btn_ShowWhoRated")]
    [InlineData(AppLanguage.Portuguese, "Btn_ShowWhoRated")]
    [InlineData(AppLanguage.Indonesian, "Btn_ShowWhoRated")]
    [InlineData(AppLanguage.Russian, "Label_Name")]
    [InlineData(AppLanguage.Ukrainian, "Label_Name")]
    [InlineData(AppLanguage.English, "Label_Name")]
    [InlineData(AppLanguage.Hindi, "Label_Name")]
    [InlineData(AppLanguage.Portuguese, "Label_Name")]
    [InlineData(AppLanguage.Indonesian, "Label_Name")]
    [InlineData(AppLanguage.Russian, "Label_Age")]
    [InlineData(AppLanguage.Ukrainian, "Label_Age")]
    [InlineData(AppLanguage.English, "Label_Age")]
    [InlineData(AppLanguage.Hindi, "Label_Age")]
    [InlineData(AppLanguage.Portuguese, "Label_Age")]
    [InlineData(AppLanguage.Indonesian, "Label_Age")]
    [InlineData(AppLanguage.Russian, "Label_AgeFilters")]
    [InlineData(AppLanguage.Ukrainian, "Label_AgeFilters")]
    [InlineData(AppLanguage.English, "Label_AgeFilters")]
    [InlineData(AppLanguage.Hindi, "Label_AgeFilters")]
    [InlineData(AppLanguage.Portuguese, "Label_AgeFilters")]
    [InlineData(AppLanguage.Indonesian, "Label_AgeFilters")]
    [InlineData(AppLanguage.Russian, "Btn_Photo")]
    [InlineData(AppLanguage.Russian, "Btn_Filters")]
    [InlineData(AppLanguage.Russian, "Btn_AiBio")]
    [InlineData(AppLanguage.Russian, "Btn_CustomAgeRange")]
    [InlineData(AppLanguage.Russian, "ReportReason_18Plus")]
    [InlineData(AppLanguage.Russian, "ReportReason_Inappropriate")]
    [InlineData(AppLanguage.Russian, "ReportReason_Other")]
    [InlineData(AppLanguage.Russian, "Menu_Guide")]
    [InlineData(AppLanguage.Russian, "BotGuide_Text")]
    [InlineData(AppLanguage.Russian, "Notification_AlreadyRatedRecently")]
    [InlineData(AppLanguage.Ukrainian, "Notification_AlreadyRatedRecently")]
    [InlineData(AppLanguage.English, "Notification_AlreadyRatedRecently")]
    [InlineData(AppLanguage.Hindi, "Notification_AlreadyRatedRecently")]
    [InlineData(AppLanguage.Portuguese, "Notification_AlreadyRatedRecently")]
    [InlineData(AppLanguage.Indonesian, "Notification_AlreadyRatedRecently")]
    public void NewlyAddedKeys_ShouldReturnNonEmptyString(AppLanguage lang, string key)
    {
        var text = _loc.Get(lang, key);
        text.Should().NotBeNullOrWhiteSpace();
        text.Should().NotBe(key); // Ensure it didn't fallback to key name
    }

    [Theory]
    [InlineData(AppLanguage.Russian, TargetGender.Male, "Парня 👦")]
    [InlineData(AppLanguage.Ukrainian, TargetGender.Male, "Хлопця 👦")]
    [InlineData(AppLanguage.English, TargetGender.Male, "Guys 👦")]
    [InlineData(AppLanguage.Hindi, TargetGender.Male, "लड़के 👦")]
    [InlineData(AppLanguage.Portuguese, TargetGender.Male, "Rapazes 👦")]
    [InlineData(AppLanguage.Indonesian, TargetGender.Male, "Pria 👦")]
    [InlineData(AppLanguage.Russian, TargetGender.Female, "Девушку 👧")]
    [InlineData(AppLanguage.Ukrainian, TargetGender.Female, "Дівчину 👧")]
    [InlineData(AppLanguage.English, TargetGender.Female, "Girls 👧")]
    [InlineData(AppLanguage.Hindi, TargetGender.Female, "लड़कियां 👧")]
    [InlineData(AppLanguage.Portuguese, TargetGender.Female, "Moças 👧")]
    [InlineData(AppLanguage.Indonesian, TargetGender.Female, "Wanita 👧")]
    public void GetTargetGenderText_ShouldReturnCorrectAccusativeForm(AppLanguage lang, TargetGender target, string expected)
    {
        var result = _loc.GetTargetGenderText(lang, target);
        result.Should().Be(expected);
    }
}
