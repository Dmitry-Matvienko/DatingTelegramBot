using DatingBot.Domain.Enums;
using DatingBot.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace DatingBot.UnitTests;

public class MatchmakingLanguageIsolationTests
{
    [Fact]
    public void RussianSpeakerInBrazil_ShouldSeeRussianUkrainianEnglishAndPortuguese()
    {
        var compatible = UserProfileRepository.GetCompatibleLanguages(AppLanguage.Russian, "Бразилия");

        compatible.Should().BeEquivalentTo([
            AppLanguage.Russian,
            AppLanguage.Ukrainian,
            AppLanguage.English,
            AppLanguage.Portuguese
        ]);
        compatible.Should().NotContain(AppLanguage.Hindi);
        compatible.Should().NotContain(AppLanguage.Indonesian);
    }

    [Fact]
    public void PortugueseSpeakerInBrazil_ShouldOnlySeePortugueseAndEnglish()
    {
        var compatible = UserProfileRepository.GetCompatibleLanguages(AppLanguage.Portuguese, "Бразилия");

        // Португалоязычный пользователь в Бразилии видит только португальский и английский (не видит русский/украинский/индонезийский)
        compatible.Should().BeEquivalentTo([AppLanguage.Portuguese, AppLanguage.English]);
        compatible.Should().NotContain(AppLanguage.Russian);
        compatible.Should().NotContain(AppLanguage.Ukrainian);
        compatible.Should().NotContain(AppLanguage.Hindi);
        compatible.Should().NotContain(AppLanguage.Indonesian);
    }

    [Fact]
    public void Hindi_ShouldOnlyMatchWithHindiAndEnglish()
    {
        var compatible = UserProfileRepository.GetCompatibleLanguages(AppLanguage.Hindi, "Индия");

        compatible.Should().BeEquivalentTo([AppLanguage.Hindi, AppLanguage.English]);
        compatible.Should().NotContain(AppLanguage.Portuguese);
        compatible.Should().NotContain(AppLanguage.Indonesian);
        compatible.Should().NotContain(AppLanguage.Russian);
        compatible.Should().NotContain(AppLanguage.Ukrainian);
    }

    [Fact]
    public void Indonesian_ShouldOnlyMatchWithIndonesianAndEnglish()
    {
        var compatible = UserProfileRepository.GetCompatibleLanguages(AppLanguage.Indonesian, "Индонезия");

        compatible.Should().BeEquivalentTo([AppLanguage.Indonesian, AppLanguage.English]);
        compatible.Should().NotContain(AppLanguage.Hindi);
        compatible.Should().NotContain(AppLanguage.Portuguese);
        compatible.Should().NotContain(AppLanguage.Russian);
    }

    [Fact]
    public void RussianInRussia_ShouldMatchWithRussianUkrainianAndEnglish()
    {
        var compatible = UserProfileRepository.GetCompatibleLanguages(AppLanguage.Russian, "Россия");

        compatible.Should().BeEquivalentTo([AppLanguage.Russian, AppLanguage.Ukrainian, AppLanguage.English]);
        compatible.Should().NotContain(AppLanguage.Hindi);
        compatible.Should().NotContain(AppLanguage.Portuguese);
        compatible.Should().NotContain(AppLanguage.Indonesian);
    }

    [Fact]
    public void UkrainianInUkraine_ShouldMatchWithUkrainianRussianAndEnglish()
    {
        var compatible = UserProfileRepository.GetCompatibleLanguages(AppLanguage.Ukrainian, "Украина");

        compatible.Should().BeEquivalentTo([AppLanguage.Ukrainian, AppLanguage.Russian, AppLanguage.English]);
        compatible.Should().NotContain(AppLanguage.Hindi);
        compatible.Should().NotContain(AppLanguage.Portuguese);
        compatible.Should().NotContain(AppLanguage.Indonesian);
    }

    [Fact]
    public void English_ShouldMatchWithAllLanguages()
    {
        var compatible = UserProfileRepository.GetCompatibleLanguages(AppLanguage.English, "США");

        compatible.Should().BeEquivalentTo([
            AppLanguage.English,
            AppLanguage.Russian,
            AppLanguage.Ukrainian,
            AppLanguage.Hindi,
            AppLanguage.Portuguese,
            AppLanguage.Indonesian
        ]);
    }
}
