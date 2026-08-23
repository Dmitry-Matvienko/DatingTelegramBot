using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Xunit;
using User = DatingBot.Domain.Entities.User;

namespace DatingBot.UnitTests;

public class SearchPromptServiceTests
{
    private readonly Mock<ITelegramBotClient> _botClient = new();
    private readonly Mock<IRegistrationService> _registrationService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly ILocalizationService _loc = new DatingBot.Application.Services.LocalizationService();
    private readonly Mock<ILogger<SearchPromptService>> _logger = new();

    private IConfiguration CreateConfig(params long[] adminIds)
    {
        var dict = new Dictionary<string, string?>();
        for (var i = 0; i < adminIds.Length; i++)
        {
            dict[$"BotConfiguration:AdminIds:{i}"] = adminIds[i].ToString();
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    private UserProfileDto CreateSampleProfile(string? photoFileId = "valid_photo_id", string? aiDescription = "Short AI bio", string? greeting = "Hello")
    {
        return new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: 111222333,
            Username: "violator_user",
            Gender: Gender.Male,
            TargetGender: TargetGender.Female,
            Name: "Violator",
            Age: 28,
            City: "Kyiv",
            Height: 185,
            PhotoFileId: photoFileId,
            DatingTarget: DatingTarget.AdultOnly,
            AiDescription: aiDescription,
            SelectedInterests: [new InterestDto(1, InterestType.Sports, "Спорт", "⚽", true)],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.None,
            SearchMinAge: null,
            SearchMaxAge: null,
            RatingCount: 3,
            AverageRating: 4.5,
            CityId: null,
            AiVector: null,
            Greeting: greeting
        );
    }

    [Fact]
    public async Task SendReportToAdminsAsync_WhenNoAdminIdsConfigured_ShouldNotSendAnyMessages()
    {
        var config = CreateConfig(); // Empty admin list
        var sut = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _logger.Object
        );

        var report = new ReportInfo(
            ReportId: Guid.NewGuid(),
            ReportedProfile: CreateSampleProfile(),
            ReporterTelegramId: 555666777,
            ReporterUsername: "reporter_user",
            ReporterFirstName: "John",
            Reason: ReportReason.InappropriateContent,
            Details: "Spam profile"
        );

        await sut.SendReportToAdminsAsync(report);

        _botClient.Verify(b => b.SendRequest(It.IsAny<IRequest<Message>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendReportToAdminsAsync_WhenPhotoValid_ShouldSendPhotoCardAndReportDetailsToAdmin()
    {
        const long adminId = 670333173;
        var config = CreateConfig(adminId);
        var sut = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _logger.Object
        );

        var report = new ReportInfo(
            ReportId: Guid.NewGuid(),
            ReportedProfile: CreateSampleProfile("photo_12345"),
            ReporterTelegramId: 555666777,
            ReporterUsername: "reporter_user",
            ReporterFirstName: "John",
            Reason: ReportReason.InappropriateContent,
            Details: "Nsfw content"
        );

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 100 });

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 101 });

        await sut.SendReportToAdminsAsync(report);

        // Photo card sent
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendPhotoRequest>(r => r.ChatId.Identifier == adminId),
            It.IsAny<CancellationToken>()), Times.Once);

        // Report moderation message sent
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId.Identifier == adminId && r.Text.Contains("ЖАЛОБА НА АНКЕТУ")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendReportToAdminsAsync_WhenSendPhotoFailsWithBadFileId_ShouldFallbackToSendMessageForCard()
    {
        const long adminId = 670333173;
        var config = CreateConfig(adminId);
        var sut = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _logger.Object
        );

        var report = new ReportInfo(
            ReportId: Guid.NewGuid(),
            ReportedProfile: CreateSampleProfile("AgACAgIAAxkBAAIOXGqI16-qmYzQWUXsTTIXJBjjJ81mAALuHGsbcpRISDD23-n1hLjQAQADAgADeQADPQQ"),
            ReporterTelegramId: 555666777,
            ReporterUsername: "reporter_user",
            ReporterFirstName: "John",
            Reason: ReportReason.InappropriateContent,
            Details: "Invalid photo identifier test"
        );

        // Telegram Bot API returns 400 Bad Request: wrong file identifier
        _botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiRequestException("Bad Request: wrong file identifier/HTTP URL specified", 400));

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 101 });

        // Act
        var act = () => sut.SendReportToAdminsAsync(report);

        // Assert - does not throw
        await act.Should().NotThrowAsync();

        // Tried to send photo once
        _botClient.Verify(b => b.SendRequest(
            It.IsAny<SendPhotoRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Fallback message with profile card sent
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId.Identifier == adminId && r.Text.Contains("Анкета пользователя")),
            It.IsAny<CancellationToken>()), Times.Once);

        // Moderation report message with buttons sent
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId.Identifier == adminId && r.Text.Contains("ЖАЛОБА НА АНКЕТУ")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendReportToAdminsAsync_WhenCaptionExceeds1024Chars_ShouldFallbackToSendMessageForCard()
    {
        const long adminId = 670333173;
        var config = CreateConfig(adminId);
        var sut = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _logger.Object
        );

        // Very long AI description and greeting exceeding 1024 chars
        var longAi = new string('A', 800);
        var longGreeting = new string('B', 300);

        var report = new ReportInfo(
            ReportId: Guid.NewGuid(),
            ReportedProfile: CreateSampleProfile("valid_photo_id", longAi, longGreeting),
            ReporterTelegramId: 555666777,
            ReporterUsername: "reporter_user",
            ReporterFirstName: "John",
            Reason: ReportReason.Other,
            Details: "Long bio report"
        );

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 101 });

        await sut.SendReportToAdminsAsync(report);

        // Should fallback to SendMessage for card and not attempt SendPhoto with >1024 caption
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId.Identifier == adminId && r.Text.Contains("Анкета пользователя")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMatchCandidateCardAsync_WhenPhotoFailsWithBadFileId_ShouldFallbackToSendMessage()
    {
        const long chatId = 123456789;
        var user = new User { TelegramId = chatId, Language = AppLanguage.Russian };
        _userRepository.Setup(r => r.GetByTelegramIdAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var config = CreateConfig();
        var sut = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _logger.Object
        );

        var match = new MatchCandidateDto(
            Profile: CreateSampleProfile("bad_file_id"),
            CommonInterests: [],
            OtherInterests: [],
            Tier: MatchTier.SameCity,
            MatchReasonBadge: "🔥 Отличное совпадение",
            SimilarityScore: 0.85,
            DistanceKm: null
        );

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiRequestException("Bad Request: wrong file identifier/HTTP URL specified", 400));

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 200 });

        await sut.SendMatchCandidateCardAsync(chatId, match);

        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId.Identifier == chatId && r.Text.Contains("Violator")),
            It.IsAny<CancellationToken>()), Times.Once);

        _registrationService.Verify(r => r.SaveLastBotMessageIdAsync(chatId, 200, It.IsAny<CancellationToken>()), Times.Once);
    }
}
