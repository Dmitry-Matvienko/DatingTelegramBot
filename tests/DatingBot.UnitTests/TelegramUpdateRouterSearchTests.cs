using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
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

public class TelegramUpdateRouterSearchTests
{
    private readonly Mock<ITelegramBotClient> _botClient = new();
    private readonly Mock<IRegistrationService> _registrationService = new();
    private readonly Mock<ISearchService> _searchService = new();
    private readonly Mock<IAdminService> _adminService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ICityRepository> _cityRepository = new();
    private readonly Mock<IProfileEditingService> _editingService = new();
    private readonly Mock<IModerationService> _moderationService = new();
    private readonly Mock<IReferralService> _referralService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ILocalizationService _loc = new LocalizationService();

    private readonly TelegramUpdateRouter _router;

    public TelegramUpdateRouterSearchTests()
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
            new Mock<IGeocodingService>().Object,
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
            new Mock<IGeocodingService>().Object,
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
            config,
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

        var referralPromptService = new ReferralPromptService(
            _botClient.Object,
            _referralService.Object,
            _loc
        );

        _router = new TelegramUpdateRouter(
            _botClient.Object,
            _registrationService.Object,
            _searchService.Object,
            _adminService.Object,
            _moderationService.Object,
            _loc,
            config,
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
            _referralService.Object,
            referralPromptService,
            routerLogger.Object
        );
    }

    [Theory]
    [InlineData("➡️ Далее")]
    [InlineData("➡️ Далі")]
    [InlineData("➡️ Next")]
    [InlineData("➡️ आगे")]
    [InlineData("➡️ Próximo")]
    [InlineData("➡️ Selanjutnya")]
    public async Task RouteUpdateAsync_WhenUserInSearchingStateAndPressesNext_ShouldCallShowNextCandidateOrIncoming(string nextButtonText)
    {
        // Arrange
        const long telegramId = 111222333L;
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            State = UserState.Searching_ViewingIncoming,
            Language = AppLanguage.Russian
        };

        _registrationService.Setup(r => r.GetOrCreateUserAsync(telegramId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepository.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _searchService.Setup(s => s.GetNextIncomingRatingAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncomingRatingDto?)null);
        _searchService.Setup(s => s.GetNextMatchCandidateAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchCandidateDto?)null);

        var update = new Update
        {
            Id = 1,
            Message = new Message
            {
                Id = 10,
                Text = nextButtonText,
                Chat = new Chat { Id = telegramId },
                From = new Telegram.Bot.Types.User { Id = telegramId }
            }
        };

        // Act
        await _router.RouteUpdateAsync(update);

        // Assert
        _searchService.Verify(s => s.GetNextIncomingRatingAsync(telegramId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
