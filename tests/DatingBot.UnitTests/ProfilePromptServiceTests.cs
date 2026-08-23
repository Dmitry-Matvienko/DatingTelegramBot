using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Xunit;
using User = DatingBot.Domain.Entities.User;

namespace DatingBot.UnitTests;

public class ProfilePromptServiceTests
{
    private readonly Mock<ITelegramBotClient> _botClient = new();
    private readonly Mock<IRegistrationService> _registrationService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ILogger<ProfilePromptService>> _logger = new();
    private readonly Mock<ILogger<RegistrationPromptService>> _regLogger = new();
    private readonly ILocalizationService _loc = new DatingBot.Application.Services.LocalizationService();

    private readonly RegistrationPromptService _registrationPromptService;
    private readonly ProfilePromptService _profilePromptService;

    public ProfilePromptServiceTests()
    {
        _registrationPromptService = new RegistrationPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            _regLogger.Object
        );

        _profilePromptService = new ProfilePromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            _registrationPromptService,
            _logger.Object
        );
    }

    [Fact]
    public async Task SendProfileCardAsync_WhenSendPhotoFailsWithBadFileId_ShouldFallbackToSendMessage()
    {
        // Arrange
        const long chatId = 123456;
        var user = new User { TelegramId = chatId, Language = AppLanguage.Russian };
        _userRepository.Setup(r => r.GetByTelegramIdAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var profile = new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: chatId,
            Username: "tester",
            Gender: Gender.Male,
            TargetGender: TargetGender.Female,
            Name: "Alex",
            Age: 25,
            City: "Kyiv",
            Height: 180,
            PhotoFileId: "invalid_or_expired_photo_file_id",
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "Ai bio",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.Age18To25 | AgeCategoryFilter.Age25To30,
            SearchMinAge: 18,
            SearchMaxAge: 30,
            RatingCount: 4,
            AverageRating: 8.5,
            CityId: null,
            AiVector: null,
            Greeting: "Hello there!"
        );

        // SendPhoto fails with 400 Bad Request: wrong file identifier
        _botClient.Setup(b => b.SendRequest(
            It.IsAny<SendPhotoRequest>(),
            It.IsAny<CancellationToken>()
        )).ThrowsAsync(new ApiRequestException("Bad Request: wrong file identifier/HTTP URL specified", 400));

        // SendMessage succeeds and returns message
        var fallbackMessage = new Message { Id = 999 };
        _botClient.Setup(b => b.SendRequest(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(fallbackMessage);

        // Act
        var act = () => _profilePromptService.SendProfileCardAsync(chatId, profile);

        // Assert - should not throw, should call SendPhoto first then fallback to SendMessage
        await act.Should().NotThrowAsync();

        _botClient.Verify(b => b.SendRequest(
            It.IsAny<SendPhotoRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId.Identifier == chatId && r.Text.Contains("Alex")),
            It.IsAny<CancellationToken>()), Times.Once);

        _registrationService.Verify(r => r.SaveLastBotMessageIdAsync(chatId, 999, It.IsAny<CancellationToken>()), Times.Once);
    }
}
