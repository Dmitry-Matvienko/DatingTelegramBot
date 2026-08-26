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
    public async Task SendMyReferralLinksAsync_WhenUserHasLink_ShouldSendMonospaceLink()
    {
        // Arrange
        var dto = new ReferralLinkDto(Guid.NewGuid(), "ref_code1", "https://t.me/DatingBot?start=ref_code1", 3, DateTime.UtcNow);
        _referralService.Setup(s => s.GetUserReferralLinkAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ReferralLinkDto?>.Success(dto));

        // Act
        await _service.SendMyReferralLinksAsync(12345, AppLanguage.Russian, 12345);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId == 12345 &&
                r.ParseMode == ParseMode.Html &&
                r.Text == "<code>https://t.me/DatingBot?start=ref_code1</code>"),
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
}
