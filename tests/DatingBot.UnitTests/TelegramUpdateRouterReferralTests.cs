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
using Telegram.Bot.Types.Enums;
using Xunit;
using User = DatingBot.Domain.Entities.User;

namespace DatingBot.UnitTests;

public class TelegramUpdateRouterReferralTests
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

    public TelegramUpdateRouterReferralTests()
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

    [Fact]
    public async Task RouteUpdateAsync_WhenReferralMenuButtonPressed_ShouldSendReferralInfo()
    {
        // Arrange
        const long chatId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = chatId, Language = AppLanguage.Russian, State = UserState.Active };

        _registrationService.Setup(r => r.GetOrCreateUserAsync(chatId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var update = new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = chatId },
                From = new Telegram.Bot.Types.User { Id = chatId },
                Text = "🎁 Реферальная программа"
            }
        };

        // Act
        await _router.RouteUpdateAsync(update);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId == chatId && r.Text.Contains("Приведите новых пользователей")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task RouteUpdateAsync_WhenCallbackRefMyLinks_AndUserHasNoLink_ShouldSendNoLinksYetMessage()
    {
        // Arrange
        const long chatId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = chatId, Language = AppLanguage.Russian, State = UserState.Active };

        _registrationService.Setup(r => r.GetOrCreateUserAsync(chatId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _referralService.Setup(s => s.GetUserReferralLinkAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Application.Common.Result<ReferralLinkDto?>.Success(null));

        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb_1",
                From = new Telegram.Bot.Types.User { Id = chatId },
                Message = new Message { Chat = new Chat { Id = chatId } },
                Data = "ref_my_links"
            }
        };

        // Act
        await _router.RouteUpdateAsync(update);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<AnswerCallbackQueryRequest>(r => r.CallbackQueryId == "cb_1"),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId == chatId && r.Text.Contains("<code>У вас еще нет ссылок</code>")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task RouteUpdateAsync_WhenCallbackRefCreateLink_ShouldCreateLinkAndSendWithPrefix()
    {
        // Arrange
        const long chatId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = chatId, Language = AppLanguage.Russian, State = UserState.Active };
        var linkDto = new ReferralLinkDto(Guid.NewGuid(), "ref_xyz", "https://t.me/DatingBot?start=ref_xyz", 0, DateTime.UtcNow);

        _registrationService.Setup(r => r.GetOrCreateUserAsync(chatId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _referralService.Setup(s => s.CreateOrGetReferralLinkAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Application.Common.Result<ReferralLinkDto>.Success(linkDto));

        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb_2",
                From = new Telegram.Bot.Types.User { Id = chatId },
                Message = new Message { Chat = new Chat { Id = chatId } },
                Data = "ref_create_link"
            }
        };

        // Act
        await _router.RouteUpdateAsync(update);

        // Assert
        _botClient.Verify(b => b.SendRequest(
            It.Is<AnswerCallbackQueryRequest>(r => r.CallbackQueryId == "cb_2"),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId == chatId && r.Text.Contains("Вот ваша реферальная ссылка, будь всегда в топе") && r.Text.Contains("<code>https://t.me/DatingBot?start=ref_xyz</code>")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task RouteUpdateAsync_WhenStartWithReferralPayload_ShouldProcessReferralAndNotifyReferrer()
    {
        // Arrange
        const long newChatId = 77777;
        const long referrerTelegramId = 99999;
        var newUser = new User { Id = Guid.NewGuid(), TelegramId = newChatId, Language = AppLanguage.Russian, State = UserState.None };

        _registrationService.Setup(r => r.GetOrCreateUserAsync(newChatId, It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);

        _referralService.Setup(s => s.ProcessReferralJoinAsync(newChatId, "ref_secret123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Application.Common.Result<ReferralProcessedDto?>.Success(new ReferralProcessedDto(referrerTelegramId, AppLanguage.Russian, 6)));

        var update = new Update
        {
            Message = new Message
            {
                Chat = new Chat { Id = newChatId },
                From = new Telegram.Bot.Types.User { Id = newChatId },
                Text = "/start ref_secret123"
            }
        };

        // Act
        await _router.RouteUpdateAsync(update);

        // Assert
        _referralService.Verify(s => s.ProcessReferralJoinAsync(newChatId, "ref_secret123", It.IsAny<CancellationToken>()), Times.Once);

        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId == referrerTelegramId && r.Text.Contains("По вашей ссылке перешёл новый пользователь") && r.Text.Contains("6")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
