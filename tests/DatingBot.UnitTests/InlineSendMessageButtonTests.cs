using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Bot.Keyboards;
using DatingBot.Bot.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;
using User = DatingBot.Domain.Entities.User;

namespace DatingBot.UnitTests;

public class InlineSendMessageButtonTests
{
    private readonly LocalizationService _loc = new();

    [Theory]
    [InlineData(123456789L, "durov", "https://t.me/durov")]
    [InlineData(123456789L, "@durov", "https://t.me/durov")]
    [InlineData(987654321L, "  test_user  ", "https://t.me/test_user")]
    public void TelegramUrlHelper_WhenUsernameIsProvided_ShouldReturnHttpsTmeUrl(long telegramId, string? username, string expectedUrl)
    {
        var url = TelegramUrlHelper.GetUserProfileUrl(telegramId, username);
        url.Should().Be(expectedUrl);
    }

    [Theory]
    [InlineData(123456789L, null, "tg://user?id=123456789")]
    [InlineData(123456789L, "", "tg://user?id=123456789")]
    [InlineData(987654321L, "   ", "tg://user?id=987654321")]
    public void TelegramUrlHelper_WhenUsernameIsEmptyOrNull_ShouldReturnTgUserDeepLink(long telegramId, string? username, string expectedUrl)
    {
        var url = TelegramUrlHelper.GetUserProfileUrl(telegramId, username);
        url.Should().Be(expectedUrl);
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "💬 Написать")]
    [InlineData(AppLanguage.Ukrainian, "💬 Написати")]
    [InlineData(AppLanguage.English, "💬 Message")]
    [InlineData(AppLanguage.Hindi, "💬 संदेश भेजें")]
    [InlineData(AppLanguage.Portuguese, "💬 Enviar mensagem")]
    [InlineData(AppLanguage.Indonesian, "💬 Kirim pesan")]
    public void LocalizationService_Btn_SendMessage_ShouldBeConfiguredForAllLanguages(AppLanguage lang, string expectedText)
    {
        var text = _loc.Get(lang, "Btn_SendMessage");
        text.Should().Be(expectedText);
    }

    [Fact]
    public void SearchKeyboards_GetMutualMatchKeyboard_ShouldReturnInlineKeyboardMarkupWithSendMessageButton()
    {
        var keyboard = SearchKeyboards.GetMutualMatchKeyboard(123456789L, "match_partner", AppLanguage.Russian);

        keyboard.Should().NotBeNull();
        var buttons = keyboard.InlineKeyboard.ToList();
        buttons.Should().HaveCount(1);
        buttons[0].Should().HaveCount(1);

        var button = buttons[0].First();
        button.Text.Should().Be("💬 Написать");
        button.Url.Should().Be("https://t.me/match_partner");
    }

    [Fact]
    public void SearchKeyboards_GetRaterCardKeyboard_ShouldReturnInlineKeyboardMarkupWithSendMessageButton()
    {
        var keyboard = SearchKeyboards.GetRaterCardKeyboard(987654321L, null, AppLanguage.Russian);

        keyboard.Should().NotBeNull();
        var buttons = keyboard.InlineKeyboard.ToList();
        buttons.Should().HaveCount(1);
        buttons[0].Should().HaveCount(1);

        var button = buttons[0].First();
        button.Text.Should().Be("💬 Написать");
        button.Url.Should().Be("tg://user?id=987654321");
    }

    [Fact]
    public void AdminKeyboards_GetAdminProfileCardKeyboard_ShouldIncludeSendMessageButtonAsFirstRow()
    {
        var userId = Guid.NewGuid();
        var keyboard = AdminKeyboards.GetAdminProfileCardKeyboard(userId, 555666777L, "admin_view_user", Gender.Female, 1, AppLanguage.Russian);

        keyboard.Should().NotBeNull();
        var rows = keyboard.InlineKeyboard.ToList();
        rows.Should().HaveCount(3);

        // Row 1: Send Message
        rows[0].Should().HaveCount(1);
        rows[0].First().Text.Should().Be("💬 Написать");
        rows[0].First().Url.Should().Be("https://t.me/admin_view_user");

        // Row 2: Block & Delete
        rows[1].Should().HaveCount(2);

        // Row 3: Next
        rows[2].Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchPromptService_SendMutualMatchNotificationAsync_ShouldIncludeInlineKeyboardWithSendMessageButton()
    {
        var botClient = new Mock<ITelegramBotClient>();
        var registrationService = new Mock<IRegistrationService>();
        var userRepo = new Mock<IUserRepository>();
        var config = new ConfigurationBuilder().Build();
        var logger = new Mock<ILogger<SearchPromptService>>();

        var recipientId = 111222333L;
        userRepo.Setup(r => r.GetByTelegramIdAsync(recipientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = recipientId, Language = AppLanguage.Russian });

        var partner = new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: 999888777L,
            Username: "partner_user",
            Gender: Gender.Female,
            TargetGender: TargetGender.Male,
            Name: "Anna",
            Age: 24,
            City: "Kyiv",
            Height: 168,
            PhotoFileId: "partner_photo",
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "Partner bio",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.None,
            SearchMinAge: null,
            SearchMaxAge: null,
            RatingCount: 10,
            AverageRating: 8.5,
            CityId: null,
            AiVector: null,
            Greeting: "Hi there!"
        );

        botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 100 });

        var sut = new SearchPromptService(botClient.Object, registrationService.Object, userRepo.Object, _loc, config, logger.Object);

        await sut.SendMutualMatchNotificationAsync(recipientId, partner, 9, 8);

        botClient.Verify(b => b.SendRequest(
            It.Is<SendPhotoRequest>(r =>
                r.ChatId.Identifier == recipientId &&
                r.ReplyMarkup is InlineKeyboardMarkup &&
                ((InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.First().First().Url == "https://t.me/partner_user"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchPromptService_SendRaterCardAsync_ShouldIncludeInlineKeyboardWithSendMessageButton()
    {
        var botClient = new Mock<ITelegramBotClient>();
        var registrationService = new Mock<IRegistrationService>();
        var userRepo = new Mock<IUserRepository>();
        var config = new ConfigurationBuilder().Build();
        var logger = new Mock<ILogger<SearchPromptService>>();

        var chatId = 111222333L;
        userRepo.Setup(r => r.GetByTelegramIdAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = chatId, Language = AppLanguage.Russian });

        var rater = new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: 999888777L,
            Username: null,
            Gender: Gender.Female,
            TargetGender: TargetGender.Male,
            Name: "Anna",
            Age: 24,
            City: "Kyiv",
            Height: 168,
            PhotoFileId: "rater_photo",
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "Rater bio",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.None,
            SearchMinAge: null,
            SearchMaxAge: null,
            RatingCount: 10,
            AverageRating: 8.5,
            CityId: null,
            AiVector: null,
            Greeting: "Hi there!"
        );

        botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 100 });

        var sut = new SearchPromptService(botClient.Object, registrationService.Object, userRepo.Object, _loc, config, logger.Object);

        await sut.SendRaterCardAsync(chatId, rater, 8);

        botClient.Verify(b => b.SendRequest(
            It.Is<SendPhotoRequest>(r =>
                r.ChatId.Identifier == chatId &&
                r.ReplyMarkup is InlineKeyboardMarkup &&
                ((InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.First().First().Url == "tg://user?id=999888777"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdminPromptService_SendAdminCandidateCardAsync_ShouldIncludeInlineKeyboardWithSendMessageButton()
    {
        var botClient = new Mock<ITelegramBotClient>();
        var adminService = new Mock<IAdminService>();
        var registrationService = new Mock<IRegistrationService>();
        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<AdminPromptService>>();

        var chatId = 111222333L;
        userRepo.Setup(r => r.GetByTelegramIdAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = chatId, Language = AppLanguage.Russian });

        var candidate = new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: 777666555L,
            Username: "candidate_username",
            Gender: Gender.Male,
            TargetGender: TargetGender.Female,
            Name: "Alex",
            Age: 29,
            City: "Warsaw",
            Height: 180,
            PhotoFileId: "admin_cand_photo",
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "Admin candidate bio",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.None,
            SearchMinAge: null,
            SearchMaxAge: null,
            RatingCount: 5,
            AverageRating: 7.0,
            CityId: null,
            AiVector: null,
            Greeting: null
        );

        botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 100 });

        var sut = new AdminPromptService(botClient.Object, adminService.Object, registrationService.Object, userRepo.Object, _loc, logger.Object);

        await sut.SendAdminCandidateCardAsync(chatId, candidate, Gender.Male, 1, 10, 2);

        botClient.Verify(b => b.SendRequest(
            It.Is<SendPhotoRequest>(r =>
                r.ChatId.Identifier == chatId &&
                r.ReplyMarkup is InlineKeyboardMarkup &&
                ((InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.First().First().Url == "https://t.me/candidate_username"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchPromptService_SendMutualMatchNotificationAsync_WhenNoPhoto_ShouldFallbackToSendMessageWithInlineKeyboard()
    {
        var botClient = new Mock<ITelegramBotClient>();
        var registrationService = new Mock<IRegistrationService>();
        var userRepo = new Mock<IUserRepository>();
        var config = new ConfigurationBuilder().Build();
        var logger = new Mock<ILogger<SearchPromptService>>();

        var recipientId = 111222333L;
        userRepo.Setup(r => r.GetByTelegramIdAsync(recipientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = recipientId, Language = AppLanguage.Russian });

        var partner = new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: 999888777L,
            Username: "partner_user",
            Gender: Gender.Female,
            TargetGender: TargetGender.Male,
            Name: "Anna",
            Age: 24,
            City: "Kyiv",
            Height: 168,
            PhotoFileId: null,
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "Partner bio",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.None,
            SearchMinAge: null,
            SearchMaxAge: null,
            RatingCount: 10,
            AverageRating: 8.5,
            CityId: null,
            AiVector: null,
            Greeting: "Hi there!"
        );

        botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 100 });

        var sut = new SearchPromptService(botClient.Object, registrationService.Object, userRepo.Object, _loc, config, logger.Object);

        await sut.SendMutualMatchNotificationAsync(recipientId, partner, 9, 8);

        botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId.Identifier == recipientId &&
                r.ReplyMarkup is InlineKeyboardMarkup &&
                ((InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.First().First().Url == "https://t.me/partner_user"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchPromptService_SendRaterCardAsync_WhenNoPhoto_ShouldFallbackToSendMessageWithInlineKeyboard()
    {
        var botClient = new Mock<ITelegramBotClient>();
        var registrationService = new Mock<IRegistrationService>();
        var userRepo = new Mock<IUserRepository>();
        var config = new ConfigurationBuilder().Build();
        var logger = new Mock<ILogger<SearchPromptService>>();

        var chatId = 111222333L;
        userRepo.Setup(r => r.GetByTelegramIdAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = chatId, Language = AppLanguage.Russian });

        var rater = new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: 999888777L,
            Username: null,
            Gender: Gender.Female,
            TargetGender: TargetGender.Male,
            Name: "Anna",
            Age: 24,
            City: "Kyiv",
            Height: 168,
            PhotoFileId: null,
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "Rater bio",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.None,
            SearchMinAge: null,
            SearchMaxAge: null,
            RatingCount: 10,
            AverageRating: 8.5,
            CityId: null,
            AiVector: null,
            Greeting: "Hi there!"
        );

        botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 100 });

        var sut = new SearchPromptService(botClient.Object, registrationService.Object, userRepo.Object, _loc, config, logger.Object);

        await sut.SendRaterCardAsync(chatId, rater, 8);

        botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId.Identifier == chatId &&
                r.ReplyMarkup is InlineKeyboardMarkup &&
                ((InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.First().First().Url == "tg://user?id=999888777"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
