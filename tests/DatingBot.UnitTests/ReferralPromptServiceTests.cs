using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Bot.Services;
using DatingBot.Domain.Enums;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace DatingBot.UnitTests;

public class ReferralPromptServiceTests
{
    private readonly Mock<ITelegramBotClient> _botClient = new();
    private readonly Mock<IReferralService> _referralService = new();
    private readonly ILocalizationService _loc = new LocalizationService();

    private readonly ReferralPromptService _service;

    public ReferralPromptServiceTests()
    {
        _service = new ReferralPromptService(
            _botClient.Object,
            _referralService.Object,
            _loc
        );
    }

    [Theory]
    [InlineData(AppLanguage.Russian)]
    [InlineData(AppLanguage.Ukrainian)]
    [InlineData(AppLanguage.English)]
    [InlineData(AppLanguage.Hindi)]
    [InlineData(AppLanguage.Portuguese)]
    [InlineData(AppLanguage.Indonesian)]
    public async Task SendReferralProgramInfoAsync_ShouldSendLocalizedMessageWithInlineKeyboard(AppLanguage language)
    {
        // Act
        await _service.SendReferralProgramInfoAsync(12345, language);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId == 12345 &&
                r.ParseMode == ParseMode.Html &&
                r.ReplyMarkup != null &&
                r.Text == _loc.Get(language, "Referral_Info")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendMyReferralLinksAsync_WhenUserHasNoLink_ShouldSendMonospaceNoLinksMessage()
    {
        // Arrange
        _referralService.Setup(s => s.GetUserReferralLinkAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReferralLinkDto?>.Success(null));

        // Act
        await _service.SendMyReferralLinksAsync(12345, AppLanguage.Russian, 12345);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId == 12345 &&
                r.ParseMode == ParseMode.Html &&
                r.Text.Contains("<code>У вас еще нет ссылок</code>")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendMyReferralLinksAsync_WhenUserHasLink_ShouldSendStatsAndMonospaceLink()
    {
        // Arrange
        var dto = new ReferralLinkDto(Guid.NewGuid(), "ref_code1", "https://t.me/DatingBot?start=ref_code1", 3, DateTime.UtcNow, RemainingBoostDays: 7);
        _referralService.Setup(s => s.GetUserReferralLinkAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReferralLinkDto?>.Success(dto));

        // Act
        await _service.SendMyReferralLinksAsync(12345, AppLanguage.Russian, 12345);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId == 12345 &&
                r.ParseMode == ParseMode.Html &&
                r.Text.Contains("Приведено пользователей: <b>3</b>") &&
                r.Text.Contains("Дней в топе поиска осталось: <b>7</b>") &&
                r.Text.Contains("<code>https://t.me/DatingBot?start=ref_code1</code>")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendCreateReferralLinkAsync_ShouldSendPrefixWithMonospaceLink()
    {
        // Arrange
        var dto = new ReferralLinkDto(Guid.NewGuid(), "ref_newcode", "https://t.me/DatingBot?start=ref_newcode", 0, DateTime.UtcNow);
        _referralService.Setup(s => s.CreateOrGetReferralLinkAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReferralLinkDto>.Success(dto));

        // Act
        await _service.SendCreateReferralLinkAsync(12345, AppLanguage.Russian, 12345);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId == 12345 &&
                r.ParseMode == ParseMode.Html &&
                r.Text.Contains("Вот ваша реферальная ссылка, будь всегда в топе") &&
                r.Text.Contains("<code>https://t.me/DatingBot?start=ref_newcode</code>")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendReferralProgramInfoAsync_WhenIsAdminTrue_ShouldIncludeReportButton()
    {
        // Act
        await _service.SendReferralProgramInfoAsync(12345, AppLanguage.Russian, isAdmin: true);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId == 12345 &&
                r.ReplyMarkup != null &&
                ((Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.Any(row => row.Any(btn => btn.CallbackData == "ref_admin_report"))),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendReferralProgramInfoAsync_WhenIsAdminFalse_ShouldNotIncludeReportButton()
    {
        // Act
        await _service.SendReferralProgramInfoAsync(12345, AppLanguage.Russian, isAdmin: false);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId == 12345 &&
                r.ReplyMarkup != null &&
                ((Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup)r.ReplyMarkup).InlineKeyboard.All(row => row.All(btn => btn.CallbackData != "ref_admin_report"))),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendReferralReportAsync_WhenEmpty_ShouldSendEmptyMessage()
    {
        // Arrange
        _referralService.Setup(s => s.GetTopReferrersAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ReferralTopUserDto>>.Success(new List<ReferralTopUserDto>()));

        // Act
        await _service.SendReferralReportAsync(12345, AppLanguage.Russian);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId == 12345 &&
                r.ParseMode == ParseMode.Html &&
                r.Text == _loc.Get(AppLanguage.Russian, "Referral_Report_Empty")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendReferralReportAsync_WhenHasReferrers_ShouldSendFormattedListWithClickableLinksAndCounts()
    {
        // Arrange
        var list = new List<ReferralTopUserDto>
        {
            new(Guid.NewGuid(), 111, "alice_username", "Alice", 10),
            new(Guid.NewGuid(), 222, null, "Bob", 5)
        };

        _referralService.Setup(s => s.GetTopReferrersAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<ReferralTopUserDto>>.Success(list));

        // Act
        await _service.SendReferralReportAsync(12345, AppLanguage.Russian);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId == 12345 &&
                r.ParseMode == ParseMode.Html &&
                r.Text.Contains("Топ-15 пользователей по реферальной программе") &&
                r.Text.Contains("<a href=\"https://t.me/alice_username\">Alice</a> — <b>10</b> чел.") &&
                r.Text.Contains("<a href=\"tg://user?id=222\">Bob</a> — <b>5</b> чел.")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
