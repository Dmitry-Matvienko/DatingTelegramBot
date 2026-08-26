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
    [InlineData(123456789L, null)]
    [InlineData(123456789L, "")]
    [InlineData(987654321L, "   ")]
    public void TelegramUrlHelper_WhenUsernameIsEmptyOrNull_ShouldReturnNull(long telegramId, string? username)
    {
        var url = TelegramUrlHelper.GetUserProfileUrl(telegramId, username);
        url.Should().BeNull();
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

    [Theory]
    [InlineData(AppLanguage.Russian, "💬 <b>Вы можете написать этому человеку:</b>")]
    [InlineData(AppLanguage.Ukrainian, "💬 <b>Ви можете написати цій людині:</b>")]
    [InlineData(AppLanguage.English, "💬 <b>You can write to this person:</b>")]
    [InlineData(AppLanguage.Hindi, "💬 <b>आप इस व्यक्ति को संदेश भेज सकते हैं:</b>")]
    [InlineData(AppLanguage.Portuguese, "💬 <b>Você pode escrever para esta pessoa:</b>")]
    [InlineData(AppLanguage.Indonesian, "💬 <b>Anda dapat mengirim pesan ke orang ini:</b>")]
    public void LocalizationService_Notification_CanMessageUser_ShouldBeConfiguredForAllLanguages(AppLanguage lang, string expectedText)
    {
        var text = _loc.Get(lang, "Notification_CanMessageUser");
        text.Should().Be(expectedText);
    }

    [Fact]
    public void SearchKeyboards_GetMutualMatchKeyboard_WhenUsernameIsProvided_ShouldReturnInlineKeyboardMarkupWithSendMessageButton()
    {
        var keyboard = SearchKeyboards.GetMutualMatchKeyboard(123456789L, "match_partner", AppLanguage.Russian);

        keyboard.Should().NotBeNull();
        var buttons = keyboard!.InlineKeyboard.ToList();
        buttons.Should().HaveCount(1);
        buttons[0].Should().HaveCount(1);

        var button = buttons[0].First();
        button.Text.Should().Be("💬 Написать");
        button.Url.Should().Be("https://t.me/match_partner");
    }

    [Fact]
    public void SearchKeyboards_GetMutualMatchKeyboard_WhenUsernameIsNull_ShouldReturnNull()
    {
        var keyboard = SearchKeyboards.GetMutualMatchKeyboard(123456789L, null, AppLanguage.Russian);
        keyboard.Should().BeNull();
    }

    [Fact]
    public void SearchKeyboards_GetRaterCardKeyboard_WhenUsernameIsNull_ShouldReturnNull()
    {
        var keyboard = SearchKeyboards.GetRaterCardKeyboard(987654321L, null, AppLanguage.Russian);
        keyboard.Should().BeNull();
    }

    [Fact]
    public void SearchKeyboards_GetRaterCardKeyboard_WhenUsernameIsProvided_ShouldReturnKeyboard()
    {
        var keyboard = SearchKeyboards.GetRaterCardKeyboard(987654321L, "rater_user", AppLanguage.Russian);

        keyboard.Should().NotBeNull();
        var buttons = keyboard!.InlineKeyboard.ToList();
        buttons.Should().HaveCount(1);
        buttons[0].Should().HaveCount(1);

        var button = buttons[0].First();
        button.Text.Should().Be("💬 Написать");
        button.Url.Should().Be("https://t.me/rater_user");
    }

    [Fact]
    public void AdminKeyboards_GetAdminProfileCardKeyboard_WhenUsernameIsNull_ShouldNotIncludeSendMessageButton()
    {
        var keyboard = AdminKeyboards.GetAdminProfileCardKeyboard(Guid.NewGuid(), 987654321L, null, Gender.Male, 1, AppLanguage.Russian);

        keyboard.Should().NotBeNull();
        var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
        allButtons.Should().NotContain(b => b.Url != null);
        allButtons.Should().Contain(b => b.CallbackData != null && b.CallbackData.StartsWith("adm_s_ban:"));
        allButtons.Should().Contain(b => b.CallbackData != null && b.CallbackData.StartsWith("adm_s_del:"));
        allButtons.Should().Contain(b => b.CallbackData != null && b.CallbackData.StartsWith("adm_s_next:"));
    }

    [Fact]
    public void AdminKeyboards_GetAdminProfileCardKeyboard_WhenUsernameIsProvided_ShouldIncludeSendMessageButton()
    {
        var keyboard = AdminKeyboards.GetAdminProfileCardKeyboard(Guid.NewGuid(), 987654321L, "admin_viewed_user", Gender.Male, 1, AppLanguage.Russian);

        keyboard.Should().NotBeNull();
        var allButtons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();
        allButtons.Should().Contain(b => b.Url == "https://t.me/admin_viewed_user");
    }

    [Fact]
    public void SearchKeyboards_GetIncomingRatingReplyKeyboard_ShouldNotContainSearchAgainButton()
    {
        var keyboard = SearchKeyboards.GetIncomingRatingReplyKeyboard(AppLanguage.Russian);

        keyboard.Should().NotBeNull();
        var rows = keyboard.Keyboard.ToList();
        rows.Should().HaveCount(3);

        // Row 1: 1..5
        rows[0].Select(b => b.Text).Should().Equal("1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣");

        // Row 2: 6..10
        rows[1].Select(b => b.Text).Should().Equal("6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟");

        // Row 3: Report, Main Menu (NO Search Again button, NO Next button when queue has 1 or less)
        rows[2].Select(b => b.Text).Should().Equal("🚨 Пожаловаться", "🏠 Главное меню");
        rows[2].Select(b => b.Text).Should().NotContain("🔄 Искать снова");
        rows[2].Select(b => b.Text).Should().NotContain("➡️ Далее");
    }

    [Fact]
    public void SearchKeyboards_GetIncomingRatingReplyKeyboard_WhenHasMoreInQueue_ShouldIncludeNextButton()
    {
        var keyboard = SearchKeyboards.GetIncomingRatingReplyKeyboard(hasMoreInQueue: true, AppLanguage.Russian);

        keyboard.Should().NotBeNull();
        var rows = keyboard.Keyboard.ToList();
        rows.Should().HaveCount(3);

        // Row 3: Report, Next, Main Menu
        rows[2].Select(b => b.Text).Should().Equal("🚨 Пожаловаться", "➡️ Далее", "🏠 Главное меню");
    }

    [Theory]
    [InlineData(AppLanguage.Russian, "➡️ Далее")]
    [InlineData(AppLanguage.Ukrainian, "➡️ Далі")]
    [InlineData(AppLanguage.English, "➡️ Next")]
    [InlineData(AppLanguage.Hindi, "➡️ आगे")]
    [InlineData(AppLanguage.Portuguese, "➡️ Próximo")]
    [InlineData(AppLanguage.Indonesian, "➡️ Selanjutnya")]
    public void SearchKeyboards_GetIncomingRatingReplyKeyboard_ShouldLocalizeNextButton(AppLanguage lang, string expectedText)
    {
        var keyboard = SearchKeyboards.GetIncomingRatingReplyKeyboard(hasMoreInQueue: true, lang);
        var rows = keyboard.Keyboard.ToList();
        rows[2].Select(b => b.Text).Should().Contain(expectedText);
    }

    [Theory]
    [InlineData(3, AppLanguage.Russian, "👀 Показать кто оценил (3)")]
    [InlineData(1, AppLanguage.Russian, "👀 Показать кто оценил (1)")]
    [InlineData(2, AppLanguage.Ukrainian, "👀 Показати хто оцінив (2)")]
    [InlineData(5, AppLanguage.English, "👀 Show who rated (5)")]
    [InlineData(0, AppLanguage.Russian, "👀 Показать кто оценил")]
    public void SearchKeyboards_GetIncomingRatingNotificationKeyboard_ShouldIncludeCountWhenProvided(int count, AppLanguage lang, string expectedText)
    {
        var ratingId = Guid.NewGuid();
        var keyboard = SearchKeyboards.GetIncomingRatingNotificationKeyboard(count, ratingId, lang);

        keyboard.Should().NotBeNull();
        var button = keyboard.InlineKeyboard.First().First();
        button.Text.Should().Be(expectedText);
        button.CallbackData.Should().Be($"view_rater:{ratingId}");
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
    public async Task SearchPromptService_SendRaterCardAsync_ShouldSendPhotoWithRatingReplyKeyboard_AndFollowUpMessageWithInlineSendMessageButton()
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
            Username: "rater_user",
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

        botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 101 });

        var sut = new SearchPromptService(botClient.Object, registrationService.Object, userRepo.Object, _loc, config, logger.Object);

        await sut.SendRaterCardAsync(chatId, rater, 8);

        // 1. Photo card sent with 1..10 ReplyKeyboardMarkup
        botClient.Verify(b => b.SendRequest(
            It.Is<SendPhotoRequest>(r =>
                r.ChatId.Identifier == chatId &&
                r.ReplyMarkup is ReplyKeyboardMarkup),
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Follow-up message sent with InlineKeyboardMarkup (Send Message button)
        botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId.Identifier == chatId &&
                r.Text.Contains("Вы можете написать этому человеку") &&
                r.ReplyMarkup is InlineKeyboardMarkup &&
                ((InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.First().First().Url == "https://t.me/rater_user"),
            It.IsAny<CancellationToken>()), Times.Once);

        // 3. Last bot message id saved from the follow-up message
        registrationService.Verify(r => r.SaveLastBotMessageIdAsync(chatId, 101, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchPromptService_SendRaterCardAsync_WhenUsernameIsNull_ShouldNotSendFollowUpMessage()
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

        // 1. Photo card sent with 1..10 ReplyKeyboardMarkup
        botClient.Verify(b => b.SendRequest(
            It.Is<SendPhotoRequest>(r =>
                r.ChatId.Identifier == chatId &&
                r.ReplyMarkup is ReplyKeyboardMarkup),
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. No follow-up message sent because user has no username
        botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId.Identifier == chatId),
            It.IsAny<CancellationToken>()), Times.Never);

        // 3. Last bot message id saved from the photo card
        registrationService.Verify(r => r.SaveLastBotMessageIdAsync(chatId, 100, It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task AdminPromptService_SendAdminCandidateCardAsync_WhenUsernameIsNull_ShouldNotIncludeSendMessageButton()
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
            Username: null,
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
                ((InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.SelectMany(row => row).All(b => b.Url == null)),
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
    public async Task SearchPromptService_SendRaterCardAsync_WhenNoPhoto_ShouldSendTextMessageWithRatingReplyKeyboard_AndFollowUpMessageWithInlineSendMessageButton()
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
            Username: "rater_user",
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

        botClient.SetupSequence(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 100 })
            .ReturnsAsync(new Message { Id = 101 });

        var sut = new SearchPromptService(botClient.Object, registrationService.Object, userRepo.Object, _loc, config, logger.Object);

        await sut.SendRaterCardAsync(chatId, rater, 8);

        // 1. Text card sent with 1..10 ReplyKeyboardMarkup
        botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId.Identifier == chatId &&
                r.Text.Contains("Anna") &&
                r.ReplyMarkup is ReplyKeyboardMarkup),
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Follow-up message sent with InlineKeyboardMarkup
        botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId.Identifier == chatId &&
                r.Text.Contains("Вы можете написать этому человеку") &&
                r.ReplyMarkup is InlineKeyboardMarkup &&
                ((InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.First().First().Url == "https://t.me/rater_user"),
            It.IsAny<CancellationToken>()), Times.Once);

        // 3. Last bot message id saved from the follow-up message
        registrationService.Verify(r => r.SaveLastBotMessageIdAsync(chatId, 101, It.IsAny<CancellationToken>()), Times.Once);
    }
}
