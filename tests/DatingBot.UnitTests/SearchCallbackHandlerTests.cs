using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Handlers;
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
using Xunit;
using User = DatingBot.Domain.Entities.User;

namespace DatingBot.UnitTests;

public class SearchCallbackHandlerTests
{
    private readonly Mock<ITelegramBotClient> _botClient = new();
    private readonly Mock<ISearchService> _searchService = new();
    private readonly Mock<IRegistrationService> _registrationService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly ILocalizationService _loc = new DatingBot.Application.Services.LocalizationService();
    private readonly Mock<ILogger<SearchPromptService>> _searchPromptLogger = new();
    private readonly Mock<ILogger<ProfilePromptService>> _profilePromptLogger = new();
    private readonly Mock<ILogger<RegistrationPromptService>> _registrationPromptLogger = new();

    private UserProfileDto CreateSampleProfile(Guid id, long telegramId, string name)
    {
        return new UserProfileDto(
            Id: id,
            TelegramId: telegramId,
            Username: $"user_{telegramId}",
            Gender: Gender.Male,
            TargetGender: TargetGender.Female,
            Name: name,
            Age: 25,
            City: "Kyiv",
            Height: 180,
            PhotoFileId: "photo_123",
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "AI Bio",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.None,
            SearchMinAge: null,
            SearchMaxAge: null,
            RatingCount: 5,
            AverageRating: 8.0,
            CityId: null,
            AiVector: null,
            Greeting: "Hello!"
        );
    }

    [Fact]
    public async Task HandleRatingFromReplyKeyboardAsync_WhenMutualMatch_ShouldSendNotificationsAndResetToMainMenu()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        const long currentTelegramId = 111222333L;
        const long candidateTelegramId = 999888777L;

        var currentUser = new User
        {
            Id = currentUserId,
            TelegramId = currentTelegramId,
            Language = AppLanguage.Russian,
            State = UserState.Searching_ViewingIncoming,
            CurrentCandidateProfileId = candidateId
        };

        var config = new ConfigurationBuilder().Build();
        var searchPromptService = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _searchPromptLogger.Object
        );

        var registrationPromptService = new RegistrationPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            _registrationPromptLogger.Object
        );

        var profilePromptService = new ProfilePromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            registrationPromptService,
            _profilePromptLogger.Object
        );

        var raterDto = CreateSampleProfile(currentUserId, currentTelegramId, "Current User");
        var candidateDto = CreateSampleProfile(candidateId, candidateTelegramId, "Candidate User");

        var ratingResult = new RatingResult(
            RatingId: Guid.NewGuid(),
            ToTelegramId: candidateTelegramId,
            Score: 8,
            NewRatingCount: 6,
            NewAverageRating: 8.0,
            IsMutualMatch: true,
            OriginalScore: 9,
            RaterProfile: raterDto,
            CandidateProfile: candidateDto
        );

        _searchService.Setup(s => s.RateCandidateAsync(currentTelegramId, candidateId, 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RatingResult>.Success(ratingResult));

        _userRepository.Setup(r => r.GetByTelegramIdAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(candidateTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = candidateTelegramId, Language = AppLanguage.Russian });

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 200 });

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 201 });

        var handler = new SearchCallbackHandler(
            _botClient.Object,
            _searchService.Object,
            searchPromptService,
            profilePromptService,
            registrationPromptService,
            _loc
        );

        // Act
        await handler.HandleRatingFromReplyKeyboardAsync(currentTelegramId, currentUser, 8);

        // Assert
        // 1. Clear candidate called for BOTH users
        _searchService.Verify(s => s.ClearCurrentCandidateAsync(currentTelegramId, It.IsAny<CancellationToken>()), Times.Once);
        _searchService.Verify(s => s.ClearCurrentCandidateAsync(candidateTelegramId, It.IsAny<CancellationToken>()), Times.Once);

        // 2. Next candidate/incoming was NOT queried (search stopped when responding to incoming rating)
        _searchService.Verify(s => s.GetNextMatchCandidateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        _searchService.Verify(s => s.GetNextIncomingRatingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);

        // 3. Main menu message was sent with main menu reply keyboard
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId.Identifier == currentTelegramId &&
                r.Text.Contains("Главное меню") &&
                r.ReplyMarkup is Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleRatingFromReplyKeyboardAsync_WhenMutualMatchInRegularSearch_ShouldSendNotificationsAndContinueSearch()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        const long currentTelegramId = 111222333L;
        const long candidateTelegramId = 999888777L;

        var currentUser = new User
        {
            Id = currentUserId,
            TelegramId = currentTelegramId,
            Language = AppLanguage.Russian,
            State = UserState.Searching, // Regular candidate search
            CurrentCandidateProfileId = candidateId
        };

        var config = new ConfigurationBuilder().Build();
        var searchPromptService = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _searchPromptLogger.Object
        );

        var registrationPromptService = new RegistrationPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            _registrationPromptLogger.Object
        );

        var profilePromptService = new ProfilePromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            registrationPromptService,
            _profilePromptLogger.Object
        );

        var raterDto = CreateSampleProfile(currentUserId, currentTelegramId, "Current User");
        var candidateDto = CreateSampleProfile(candidateId, candidateTelegramId, "Candidate User");

        var ratingResult = new RatingResult(
            RatingId: Guid.NewGuid(),
            ToTelegramId: candidateTelegramId,
            Score: 8,
            NewRatingCount: 6,
            NewAverageRating: 8.0,
            IsMutualMatch: true,
            OriginalScore: 9,
            RaterProfile: raterDto,
            CandidateProfile: candidateDto
        );

        _searchService.Setup(s => s.RateCandidateAsync(currentTelegramId, candidateId, 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RatingResult>.Success(ratingResult));

        _userRepository.Setup(r => r.GetByTelegramIdAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(candidateTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = candidateTelegramId, Language = AppLanguage.Russian });

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 200 });

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 201 });

        var handler = new SearchCallbackHandler(
            _botClient.Object,
            _searchService.Object,
            searchPromptService,
            profilePromptService,
            registrationPromptService,
            _loc
        );

        // Act
        await handler.HandleRatingFromReplyKeyboardAsync(currentTelegramId, currentUser, 8);

        // Assert
        // 1. Clear candidate called for partner
        _searchService.Verify(s => s.ClearCurrentCandidateAsync(candidateTelegramId, It.IsAny<CancellationToken>()), Times.Once);
        // 2. Search continued for current user
        _searchService.Verify(s => s.GetNextIncomingRatingAsync(currentTelegramId, It.IsAny<CancellationToken>()), Times.Once);
        _searchService.Verify(s => s.GetNextMatchCandidateAsync(currentTelegramId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleRatingFromReplyKeyboardAsync_WhenScoreBelow6_ShouldShowNextCandidateOrIncoming()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        const long currentTelegramId = 111222333L;
        const long candidateTelegramId = 999888777L;

        var currentUser = new User
        {
            Id = currentUserId,
            TelegramId = currentTelegramId,
            Language = AppLanguage.Russian,
            State = UserState.Searching,
            CurrentCandidateProfileId = candidateId
        };

        var config = new ConfigurationBuilder().Build();
        var searchPromptService = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _searchPromptLogger.Object
        );

        var registrationPromptService = new RegistrationPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            _registrationPromptLogger.Object
        );

        var profilePromptService = new ProfilePromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            registrationPromptService,
            _profilePromptLogger.Object
        );

        var raterDto = CreateSampleProfile(currentUserId, currentTelegramId, "Current User");
        var candidateDto = CreateSampleProfile(candidateId, candidateTelegramId, "Candidate User");

        var ratingResult = new RatingResult(
            RatingId: Guid.NewGuid(),
            ToTelegramId: candidateTelegramId,
            Score: 3,
            NewRatingCount: 6,
            NewAverageRating: 7.0,
            IsMutualMatch: false,
            OriginalScore: 0,
            RaterProfile: raterDto,
            CandidateProfile: candidateDto
        );

        _searchService.Setup(s => s.RateCandidateAsync(currentTelegramId, candidateId, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RatingResult>.Success(ratingResult));

        _searchService.Setup(s => s.GetNextIncomingRatingAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncomingRatingDto?)null);

        _searchService.Setup(s => s.GetNextMatchCandidateAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchCandidateDto?)null);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 200 });

        var handler = new SearchCallbackHandler(
            _botClient.Object,
            _searchService.Object,
            searchPromptService,
            profilePromptService,
            registrationPromptService,
            _loc
        );

        // Act
        await handler.HandleRatingFromReplyKeyboardAsync(currentTelegramId, currentUser, 3);

        // Assert
        // 1. Clear candidate is NOT called (search continues)
        _searchService.Verify(s => s.ClearCurrentCandidateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);

        // 2. Next candidate search queried
        _searchService.Verify(s => s.GetNextIncomingRatingAsync(currentTelegramId, It.IsAny<CancellationToken>()), Times.Once);
        _searchService.Verify(s => s.GetNextMatchCandidateAsync(currentTelegramId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleRatingFromReplyKeyboardAsync_WhenHighRatingNotMutual_ShouldSendNotificationAndShowNextCandidate()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        const long currentTelegramId = 111222333L;
        const long candidateTelegramId = 999888777L;
        var ratingId = Guid.NewGuid();

        var currentUser = new User
        {
            Id = currentUserId,
            TelegramId = currentTelegramId,
            Language = AppLanguage.Russian,
            State = UserState.Searching,
            CurrentCandidateProfileId = candidateId
        };

        var config = new ConfigurationBuilder().Build();
        var searchPromptService = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _searchPromptLogger.Object
        );

        var registrationPromptService = new RegistrationPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            _registrationPromptLogger.Object
        );

        var profilePromptService = new ProfilePromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            registrationPromptService,
            _profilePromptLogger.Object
        );

        var raterDto = CreateSampleProfile(currentUserId, currentTelegramId, "Current User");
        var candidateDto = CreateSampleProfile(candidateId, candidateTelegramId, "Candidate User");

        var ratingResult = new RatingResult(
            RatingId: ratingId,
            ToTelegramId: candidateTelegramId,
            Score: 8,
            NewRatingCount: 6,
            NewAverageRating: 8.0,
            IsMutualMatch: false,
            OriginalScore: 0,
            RaterProfile: raterDto,
            CandidateProfile: candidateDto
        );

        _searchService.Setup(s => s.RateCandidateAsync(currentTelegramId, candidateId, 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RatingResult>.Success(ratingResult));

        _searchService.Setup(s => s.GetNextIncomingRatingAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncomingRatingDto?)null);

        _searchService.Setup(s => s.GetNextMatchCandidateAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchCandidateDto?)null);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(candidateTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = candidateTelegramId, Language = AppLanguage.Russian });

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 200 });

        var handler = new SearchCallbackHandler(
            _botClient.Object,
            _searchService.Object,
            searchPromptService,
            profilePromptService,
            registrationPromptService,
            _loc
        );

        // Act
        await handler.HandleRatingFromReplyKeyboardAsync(currentTelegramId, currentUser, 8);

        // Assert
        // 1. Notification sent to candidate with ratingId
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId.Identifier == candidateTelegramId &&
                r.ReplyMarkup is Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup),
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Next candidate search queried for current user
        _searchService.Verify(s => s.GetNextIncomingRatingAsync(currentTelegramId, It.IsAny<CancellationToken>()), Times.Once);
        _searchService.Verify(s => s.GetNextMatchCandidateAsync(currentTelegramId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleRatingFromReplyKeyboardAsync_WhenWasRecentlyRatedTrue_ShouldSendNotificationToRater()
    {
        // Arrange
        const long currentTelegramId = 11111;
        var currentUserId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        const long candidateTelegramId = 22222;

        var currentUser = new User
        {
            Id = currentUserId,
            TelegramId = currentTelegramId,
            State = UserState.Searching,
            Language = AppLanguage.Russian,
            CurrentCandidateProfileId = candidateId
        };

        var config = new ConfigurationBuilder().Build();
        var searchPromptService = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _searchPromptLogger.Object
        );

        var registrationPromptService = new RegistrationPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            _registrationPromptLogger.Object
        );

        var profilePromptService = new ProfilePromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            registrationPromptService,
            _profilePromptLogger.Object
        );

        var raterDto = CreateSampleProfile(currentUserId, currentTelegramId, "Current User");
        var candidateDto = CreateSampleProfile(candidateId, candidateTelegramId, "Candidate User");

        var ratingResult = new RatingResult(
            RatingId: Guid.NewGuid(),
            ToTelegramId: candidateTelegramId,
            Score: 4,
            NewRatingCount: 5,
            NewAverageRating: 5.0,
            IsMutualMatch: false,
            OriginalScore: 0,
            RaterProfile: raterDto,
            CandidateProfile: candidateDto,
            WasRecentlyRated: true
        );

        _searchService.Setup(s => s.RateCandidateAsync(currentTelegramId, candidateId, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RatingResult>.Success(ratingResult));

        _searchService.Setup(s => s.GetNextIncomingRatingAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncomingRatingDto?)null);

        _searchService.Setup(s => s.GetNextMatchCandidateAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchCandidateDto?)null);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 200 });

        var handler = new SearchCallbackHandler(
            _botClient.Object,
            _searchService.Object,
            searchPromptService,
            profilePromptService,
            registrationPromptService,
            _loc
        );

        // Act
        await handler.HandleRatingFromReplyKeyboardAsync(currentTelegramId, currentUser, 4);

        // Assert
        // Уведомление отправлено текущему пользователю с текстом о недавней оценке
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId.Identifier == currentTelegramId &&
                r.Text.Contains("Вы уже недавно оценивали этого пользователя")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleRatingFromReplyKeyboardAsync_WhenWasRecentlyRatedTrueAndScore6Plus_ShouldNotSendHighRatingNotificationToCandidate()
    {
        // Arrange
        const long currentTelegramId = 11111;
        var currentUserId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        const long candidateTelegramId = 22222;

        var currentUser = new User
        {
            Id = currentUserId,
            TelegramId = currentTelegramId,
            State = UserState.Searching,
            Language = AppLanguage.Russian,
            CurrentCandidateProfileId = candidateId
        };

        var config = new ConfigurationBuilder().Build();
        var searchPromptService = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            _searchPromptLogger.Object
        );

        var registrationPromptService = new RegistrationPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            _registrationPromptLogger.Object
        );

        var profilePromptService = new ProfilePromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            registrationPromptService,
            _profilePromptLogger.Object
        );

        var raterDto = CreateSampleProfile(currentUserId, currentTelegramId, "Current User");
        var candidateDto = CreateSampleProfile(candidateId, candidateTelegramId, "Candidate User");

        var ratingResult = new RatingResult(
            RatingId: Guid.NewGuid(),
            ToTelegramId: candidateTelegramId,
            Score: 8,
            NewRatingCount: 5,
            NewAverageRating: 8.0,
            IsMutualMatch: false,
            OriginalScore: 0,
            RaterProfile: raterDto,
            CandidateProfile: candidateDto,
            WasRecentlyRated: true
        );

        _searchService.Setup(s => s.RateCandidateAsync(currentTelegramId, candidateId, 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RatingResult>.Success(ratingResult));

        _searchService.Setup(s => s.GetNextIncomingRatingAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncomingRatingDto?)null);

        _searchService.Setup(s => s.GetNextMatchCandidateAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchCandidateDto?)null);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(currentTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(candidateTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = candidateTelegramId, Language = AppLanguage.Russian });

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 200 });

        var handler = new SearchCallbackHandler(
            _botClient.Object,
            _searchService.Object,
            searchPromptService,
            profilePromptService,
            registrationPromptService,
            _loc
        );

        // Act
        await handler.HandleRatingFromReplyKeyboardAsync(currentTelegramId, currentUser, 8);

        // Assert
        // 1. Уведомление отправлено текущему пользователю ("Вы уже недавно оценивали этого пользователя")
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId.Identifier == currentTelegramId &&
                r.Text.Contains("Вы уже недавно оценивали этого пользователя")),
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Уведомление кандидату НЕ отправлялось
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r =>
                r.ChatId.Identifier == candidateTelegramId),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleSearchCallbackQueryAsync_WhenViewRater_ShouldCallGetIncomingRatingByIdAsync_AndSendRaterCard()
    {
        // Arrange
        const long telegramId = 123456789L;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, Language = AppLanguage.Russian };
        var ratingId = Guid.NewGuid();
        var raterDto = CreateSampleProfile(Guid.NewGuid(), 987654321L, "Катя");
        var incomingDto = new IncomingRatingDto(ratingId, raterDto, 9, DateTime.UtcNow, RemainingQueueCount: 2);

        var config = new ConfigurationBuilder().Build();
        var searchPromptService = new SearchPromptService(_botClient.Object, _registrationService.Object, _userRepository.Object, _loc, config, _searchPromptLogger.Object);
        var registrationPromptService = new RegistrationPromptService(_botClient.Object, _registrationService.Object, _userRepository.Object, _loc, _registrationPromptLogger.Object);
        var profilePromptService = new ProfilePromptService(_botClient.Object, _registrationService.Object, _userRepository.Object, _loc, registrationPromptService, _profilePromptLogger.Object);

        _searchService.Setup(s => s.GetIncomingRatingByIdAsync(telegramId, ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incomingDto);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _botClient.Setup(b => b.SendRequest(It.IsAny<AnswerCallbackQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 101 });

        var handler = new SearchCallbackHandler(_botClient.Object, _searchService.Object, searchPromptService, profilePromptService, registrationPromptService, _loc);
        var callbackQuery = new CallbackQuery
        {
            Id = "cb_1",
            Data = $"view_rater:{ratingId}",
            Message = new Message { Id = 10, Chat = new Chat { Id = telegramId } }
        };

        // Act
        var handled = await handler.HandleSearchCallbackQueryAsync(user, callbackQuery);

        // Assert
        handled.Should().BeTrue();
        _searchService.Verify(s => s.GetIncomingRatingByIdAsync(telegramId, ratingId, It.IsAny<CancellationToken>()), Times.Once);
        _botClient.Verify(b => b.SendRequest(It.IsAny<AnswerCallbackQueryRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleSearchCallbackQueryAsync_WhenViewRaterByIdFails_ShouldFallbackToNextInQueue()
    {
        // Arrange
        const long telegramId = 123456789L;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, Language = AppLanguage.Russian };
        var ratingId = Guid.NewGuid();
        var raterDto = CreateSampleProfile(Guid.NewGuid(), 987654321L, "Оля");
        var nextIncomingDto = new IncomingRatingDto(Guid.NewGuid(), raterDto, 8, DateTime.UtcNow, RemainingQueueCount: 0);

        var config = new ConfigurationBuilder().Build();
        var searchPromptService = new SearchPromptService(_botClient.Object, _registrationService.Object, _userRepository.Object, _loc, config, _searchPromptLogger.Object);
        var registrationPromptService = new RegistrationPromptService(_botClient.Object, _registrationService.Object, _userRepository.Object, _loc, _registrationPromptLogger.Object);
        var profilePromptService = new ProfilePromptService(_botClient.Object, _registrationService.Object, _userRepository.Object, _loc, registrationPromptService, _profilePromptLogger.Object);

        // ratingId уже был просмотрен -> null
        _searchService.Setup(s => s.GetIncomingRatingByIdAsync(telegramId, ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncomingRatingDto?)null);

        // берем следующий из очереди
        _searchService.Setup(s => s.GetNextIncomingRatingAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nextIncomingDto);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _botClient.Setup(b => b.SendRequest(It.IsAny<AnswerCallbackQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _botClient.Setup(b => b.SendRequest(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 102 });

        var handler = new SearchCallbackHandler(_botClient.Object, _searchService.Object, searchPromptService, profilePromptService, registrationPromptService, _loc);
        var callbackQuery = new CallbackQuery
        {
            Id = "cb_2",
            Data = $"view_rater:{ratingId}",
            Message = new Message { Id = 20, Chat = new Chat { Id = telegramId } }
        };

        // Act
        var handled = await handler.HandleSearchCallbackQueryAsync(user, callbackQuery);

        // Assert
        handled.Should().BeTrue();
        _searchService.Verify(s => s.GetIncomingRatingByIdAsync(telegramId, ratingId, It.IsAny<CancellationToken>()), Times.Once);
        _searchService.Verify(s => s.GetNextIncomingRatingAsync(telegramId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
