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
using Telegram.Bot.Types.Enums;
using Xunit;
using User = DatingBot.Domain.Entities.User;

namespace DatingBot.UnitTests;

public class TelegramUpdateRouterStartTests
{
    private readonly Mock<ITelegramBotClient> _botClient = new();
    private readonly Mock<IRegistrationService> _registrationService = new();
    private readonly Mock<ISearchService> _searchService = new();
    private readonly Mock<IAdminService> _adminService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICityRepository> _cityRepository = new();
    private readonly Mock<IProfileEditingService> _editingService = new();
    private readonly Mock<IModerationService> _moderationService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ILocalizationService _loc = new DatingBot.Application.Services.LocalizationService();

    private readonly TelegramUpdateRouter _router;

    public TelegramUpdateRouterStartTests()
    {
        var regPromptLogger = new Mock<ILogger<RegistrationPromptService>>();
        var profilePromptLogger = new Mock<ILogger<ProfilePromptService>>();
        var searchPromptLogger = new Mock<ILogger<SearchPromptService>>();
        var adminPromptLogger = new Mock<ILogger<AdminPromptService>>();
        var adminCbLogger = new Mock<ILogger<AdminCallbackHandler>>();
        var adminBroadcastLogger = new Mock<ILogger<AdminBroadcastService>>();
        var routerLogger = new Mock<ILogger<TelegramUpdateRouter>>();

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var registrationPromptService = new RegistrationPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            regPromptLogger.Object
        );

        var profilePromptService = new ProfilePromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            registrationPromptService,
            profilePromptLogger.Object
        );

        var searchPromptService = new SearchPromptService(
            _botClient.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            config,
            searchPromptLogger.Object
        );

        var adminPromptService = new AdminPromptService(
            _botClient.Object,
            _adminService.Object,
            _registrationService.Object,
            _userRepository.Object,
            _loc,
            adminPromptLogger.Object
        );

        var adminBroadcastService = new AdminBroadcastService(
            _botClient.Object,
            _adminService.Object,
            adminBroadcastLogger.Object
        );

        var regMsgHandler = new RegistrationMessageHandler(
            _botClient.Object,
            _registrationService.Object,
            _cityRepository.Object,
            registrationPromptService,
            _loc
        );

        var regCbHandler = new RegistrationCallbackHandler(
            _botClient.Object,
            _registrationService.Object,
            _cityRepository.Object,
            registrationPromptService,
            _loc
        );

        var editMsgHandler = new ProfileEditMessageHandler(
            _botClient.Object,
            _editingService.Object,
            _registrationService.Object,
            _cityRepository.Object,
            profilePromptService,
            registrationPromptService,
            _loc
        );

        var editCbHandler = new ProfileEditCallbackHandler(
            _botClient.Object,
            _editingService.Object,
            _registrationService.Object,
            _cityRepository.Object,
            profilePromptService,
            _loc
        );

        var searchCbHandler = new SearchCallbackHandler(
            _botClient.Object,
            _searchService.Object,
            searchPromptService,
            profilePromptService,
            registrationPromptService,
            _loc
        );

        var adminCbHandler = new AdminCallbackHandler(
            _botClient.Object,
            _adminService.Object,
            _moderationService.Object,
            adminPromptService,
            adminBroadcastService,
            _userRepository.Object,
            _unitOfWork.Object,
            _loc,
            adminCbLogger.Object
        );

        var adminMsgHandler = new AdminMessageHandler(
            _botClient.Object,
            _adminService.Object,
            adminPromptService,
            adminBroadcastService,
            _userRepository.Object,
            _unitOfWork.Object,
            _loc
        );

        _router = new TelegramUpdateRouter(
            _botClient.Object,
            _registrationService.Object,
            _searchService.Object,
            _adminService.Object,
            _loc,
            regMsgHandler,
            regCbHandler,
            registrationPromptService,
            profilePromptService,
            editMsgHandler,
            editCbHandler,
            searchPromptService,
            searchCbHandler,
            adminPromptService,
            adminCbHandler,
            adminMsgHandler,
            routerLogger.Object
        );
    }

    [Fact]
    public async Task RouteUpdateAsync_WhenStartCommandReceived_ShouldNotDeleteStartMessage()
    {
        // Arrange
        const long chatId = 555123;
        const int startMessageId = 42;
        var user = new User { TelegramId = chatId, Language = AppLanguage.Russian, State = UserState.Active };

        _registrationService.Setup(r => r.GetOrCreateUserAsync(chatId, "testuser", "Test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var profile = new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: chatId,
            Username: "testuser",
            Gender: Gender.Male,
            TargetGender: TargetGender.Female,
            Name: "Test",
            Age: 22,
            City: "Moscow",
            Height: 175,
            PhotoFileId: null,
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "Ai bio",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.Age18To25,
            SearchMinAge: 18,
            SearchMaxAge: 30,
            RatingCount: 0,
            AverageRating: 0,
            CityId: null,
            AiVector: null,
            Greeting: null
        );

        _registrationService.Setup(r => r.GetProfileDtoAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var update = new Update
        {
            Id = 100,
            Message = new Message
            {
                Id = startMessageId,
                Text = "/start",
                Chat = new Chat { Id = chatId },
                From = new Telegram.Bot.Types.User { Id = chatId, Username = "testuser", FirstName = "Test" }
            }
        };

        // Act
        await _router.RouteUpdateAsync(update);

        // Assert - verify DeleteMessage was NEVER called with startMessageId
        _botClient.Verify(b => b.SendRequest(
            It.Is<DeleteMessageRequest>(r => r.ChatId.Identifier == chatId && r.MessageId == startMessageId),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
