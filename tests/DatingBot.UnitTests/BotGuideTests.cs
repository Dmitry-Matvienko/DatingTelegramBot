using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Bot.Keyboards;
using DatingBot.Bot.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;

namespace DatingBot.UnitTests;

public class BotGuideTests
{
    private readonly LocalizationService _loc = new();

    [Theory]
    [InlineData(AppLanguage.Russian)]
    [InlineData(AppLanguage.Ukrainian)]
    [InlineData(AppLanguage.English)]
    [InlineData(AppLanguage.Hindi)]
    [InlineData(AppLanguage.Portuguese)]
    [InlineData(AppLanguage.Indonesian)]
    public void MenuGuide_ShouldReturnValidStringForAllLanguages(AppLanguage lang)
    {
        var text = _loc.Get(lang, "Menu_Guide");
        text.Should().NotBeNullOrWhiteSpace();
        text.Should().NotBe("Menu_Guide");
        text.Should().StartWith("📖");
    }

    [Theory]
    [InlineData(AppLanguage.Russian)]
    [InlineData(AppLanguage.Ukrainian)]
    [InlineData(AppLanguage.English)]
    [InlineData(AppLanguage.Hindi)]
    [InlineData(AppLanguage.Portuguese)]
    [InlineData(AppLanguage.Indonesian)]
    public void BotGuideText_ShouldReturnComprehensiveGuideForAllLanguages(AppLanguage lang)
    {
        var text = _loc.Get(lang, "BotGuide_Text");
        text.Should().NotBeNullOrWhiteSpace();
        text.Should().NotBe("BotGuide_Text");
        text.Should().Contain("1–10");
        text.Should().Contain("@TheBestDating");
        text.Should().Contain("@KimeLowe65");
    }

    [Theory]
    [InlineData(AppLanguage.Russian)]
    [InlineData(AppLanguage.Ukrainian)]
    [InlineData(AppLanguage.English)]
    [InlineData(AppLanguage.Hindi)]
    [InlineData(AppLanguage.Portuguese)]
    [InlineData(AppLanguage.Indonesian)]
    public void MainMenuGreeting_ShouldContainGuideDescriptionForAllLanguages(AppLanguage lang)
    {
        var greeting = _loc.Get(lang, "MainMenuGreeting");
        greeting.Should().NotBeNullOrWhiteSpace();
        greeting.Should().Contain("📖");
    }

    [Theory]
    [InlineData(AppLanguage.Russian)]
    [InlineData(AppLanguage.Ukrainian)]
    [InlineData(AppLanguage.English)]
    [InlineData(AppLanguage.Hindi)]
    [InlineData(AppLanguage.Portuguese)]
    [InlineData(AppLanguage.Indonesian)]
    public void MainMenuReplyKeyboard_ShouldContainSearchProfileAndGuideButtons(AppLanguage lang)
    {
        var keyboard = MainMenuKeyboards.GetMainMenuReplyKeyboard(lang);
        var rows = keyboard.Keyboard.ToList();

        rows.Should().HaveCount(2);
        rows[0].Should().HaveCount(2);
        rows[0].First().Text.Should().Be(_loc.Get(lang, "Menu_Search"));
        rows[0].Last().Text.Should().Be(_loc.Get(lang, "Menu_Profile"));

        rows[1].Should().HaveCount(2);
        rows[1].First().Text.Should().Be(_loc.Get(lang, "Menu_Referral"));
        rows[1].Last().Text.Should().Be(_loc.Get(lang, "Menu_Guide"));
    }

    [Fact]
    public async Task SendBotGuideAsync_ShouldSendMessageWithBotGuideTextAndReplyKeyboard()
    {
        // Arrange
        var botClientMock = new Mock<ITelegramBotClient>();
        var registrationServiceMock = new Mock<IRegistrationService>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var loggerMock = new Mock<ILogger<ProfilePromptService>>();
        var regPromptLoggerMock = new Mock<ILogger<RegistrationPromptService>>();

        var regPromptService = new RegistrationPromptService(
            botClientMock.Object,
            registrationServiceMock.Object,
            userRepositoryMock.Object,
            _loc,
            regPromptLoggerMock.Object
        );

        var service = new ProfilePromptService(
            botClientMock.Object,
            registrationServiceMock.Object,
            userRepositoryMock.Object,
            _loc,
            regPromptService,
            loggerMock.Object
        );

        var chatId = 123456789L;
        var user = new DatingBot.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            TelegramId = chatId,
            Language = AppLanguage.Russian,
            State = UserState.Active
        };

        userRepositoryMock.Setup(r => r.GetByTelegramIdAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sentMsg = new Message { Id = 999 };
        botClientMock.Setup(b => b.SendRequest(
            It.IsAny<Telegram.Bot.Requests.SendMessageRequest>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(sentMsg);

        // Act
        await service.SendBotGuideAsync(chatId, null, CancellationToken.None);

        // Assert
        botClientMock.Verify(b => b.SendRequest(
            It.Is<Telegram.Bot.Requests.SendMessageRequest>(r =>
                r.ChatId.Identifier == chatId &&
                r.Text.Contains("Руководство пользователя DatingBot") &&
                r.Text.Contains("@TheBestDating")),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        registrationServiceMock.Verify(r => r.SaveLastBotMessageIdAsync(chatId, 999, It.IsAny<CancellationToken>()), Times.Once);
    }
}
