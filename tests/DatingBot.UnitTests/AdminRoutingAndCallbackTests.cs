using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Handlers;
using DatingBot.Bot.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;
using User = DatingBot.Domain.Entities.User;

namespace DatingBot.UnitTests;

public class AdminRoutingAndCallbackTests
{
    private readonly Mock<ITelegramBotClient> _botClient = new();
    private readonly Mock<IAdminService> _adminService = new();
    private readonly Mock<IModerationService> _moderationService = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepo = new();
    private readonly Mock<IProfileReportRepository> _reportRepo = new();
    private readonly Mock<IRegistrationService> _regService = new();
    private readonly Mock<ISearchService> _searchService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ILocalizationService _loc = new DatingBot.Application.Services.LocalizationService();

    private readonly AdminPromptService _promptService;
    private readonly AdminBroadcastService _broadcastService;
    private readonly AdminCallbackHandler _callbackHandler;
    private readonly AdminMessageHandler _messageHandler;

    public AdminRoutingAndCallbackTests()
    {
        _promptService = new AdminPromptService(
            _botClient.Object,
            _adminService.Object,
            _regService.Object,
            _userRepo.Object,
            _loc,
            new Mock<ILogger<AdminPromptService>>().Object
        );

        _broadcastService = new AdminBroadcastService(
            _botClient.Object,
            _adminService.Object,
            new Mock<ILogger<AdminBroadcastService>>().Object
        );

        _callbackHandler = new AdminCallbackHandler(
            _botClient.Object,
            _adminService.Object,
            _moderationService.Object,
            _promptService,
            _broadcastService,
            _userRepo.Object,
            _unitOfWork.Object,
            _loc,
            new Mock<ILogger<AdminCallbackHandler>>().Object
        );

        _messageHandler = new AdminMessageHandler(
            _botClient.Object,
            _adminService.Object,
            _promptService,
            _broadcastService,
            _userRepo.Object,
            _unitOfWork.Object,
            _loc
        );
    }

    [Fact]
    public async Task HandleAdminCallbackQueryAsync_ShouldDenyNonAdmin()
    {
        var user = new User { TelegramId = 999, Language = AppLanguage.Russian };
        var query = new CallbackQuery
        {
            Id = "q1",
            Data = "adm_panel:main",
            From = new Telegram.Bot.Types.User { Id = 999 }
        };

        _adminService.Setup(a => a.IsAdmin(999)).Returns(false);

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(user, query);

        handled.Should().BeTrue();
        _botClient.Verify(b => b.SendRequest(
            It.Is<AnswerCallbackQueryRequest>(r => r.ShowAlert == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAdminCallbackQueryAsync_BanInSearch_ShouldBanAndNotifyViolator()
    {
        var adminUser = new User { TelegramId = 123, Language = AppLanguage.Russian };
        var targetUserId = Guid.NewGuid();
        var query = new CallbackQuery
        {
            Id = "q2",
            Data = $"adm_s_ban:{targetUserId}:male:0",
            From = new Telegram.Bot.Types.User { Id = 123 }
        };

        _adminService.Setup(a => a.IsAdmin(123)).Returns(true);
        _adminService.Setup(a => a.BanUserDirectlyAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DatingBot.Application.Common.Result<AdminModerationActionResult>.Success(
                new AdminModerationActionResult(targetUserId, 888, AppLanguage.Ukrainian, "Тарас")
            ));
        _adminService.Setup(a => a.GetAdminProfileByGenderAsync(Gender.Male, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, 0, 0));

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(adminUser, query);

        handled.Should().BeTrue();
        _adminService.Verify(a => a.BanUserDirectlyAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);

        // Проверяем отправку уведомления нарушителю на украинском
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId.Identifier == 888 && r.Text.Contains("заблоковано")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAdminCallbackQueryAsync_DeleteInSearch_ShouldDeleteAndNotifyViolator()
    {
        var adminUser = new User { TelegramId = 123, Language = AppLanguage.Russian };
        var targetUserId = Guid.NewGuid();
        var query = new CallbackQuery
        {
            Id = "q3",
            Data = $"adm_s_del:{targetUserId}:female:1",
            From = new Telegram.Bot.Types.User { Id = 123 }
        };

        _adminService.Setup(a => a.IsAdmin(123)).Returns(true);
        _adminService.Setup(a => a.DeleteUserProfileDirectlyAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DatingBot.Application.Common.Result<AdminModerationActionResult>.Success(
                new AdminModerationActionResult(targetUserId, 777, AppLanguage.English, "Alice")
            ));
        _adminService.Setup(a => a.GetAdminProfileByGenderAsync(Gender.Female, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, 0, 0));

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(adminUser, query);

        handled.Should().BeTrue();
        _adminService.Verify(a => a.DeleteUserProfileDirectlyAsync(targetUserId, It.IsAny<CancellationToken>()), Times.Once);

        // Проверяем отправку уведомления об удалении анкеты нарушителю на английском
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId.Identifier == 777 && r.Text.Contains("deleted")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("adm_bgender:friends", DatingTarget.Friends)]
    [InlineData("adm_bgender:relationship", DatingTarget.Relationship)]
    [InlineData("adm_bgender:adult", DatingTarget.AdultOnly)]
    public async Task HandleAdminCallbackQueryAsync_SelectCategoryAudience_ShouldSetTargetGoalFilter(string callbackData, DatingTarget expectedGoal)
    {
        var adminUser = new User { TelegramId = 123, Language = AppLanguage.Russian, State = UserState.Admin_Panel };
        var query = new CallbackQuery
        {
            Id = "q4",
            Data = callbackData,
            From = new Telegram.Bot.Types.User { Id = 123 }
        };

        _adminService.Setup(a => a.IsAdmin(123)).Returns(true);

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(adminUser, query);

        handled.Should().BeTrue();
        adminUser.State.Should().Be(UserState.Admin_Broadcasting_WaitingForCity);

        var session = _broadcastService.GetOrCreateSession(123);
        session.Filter.TargetGoal.Should().Be(expectedGoal);
        session.Filter.TargetGender.Should().BeNull();
    }

    [Fact]
    public async Task HandleAdminCallbackQueryAsync_DeleteInSearch_WhenFails_ShouldShowLocalizedAlert()
    {
        var adminUser = new User { TelegramId = 123, Language = AppLanguage.Russian };
        var targetUserId = Guid.NewGuid();
        var query = new CallbackQuery
        {
            Id = "q_err_del",
            Data = $"adm_s_del:{targetUserId}:female:0",
            From = new Telegram.Bot.Types.User { Id = 123 }
        };

        _adminService.Setup(a => a.IsAdmin(123)).Returns(true);
        _adminService.Setup(a => a.DeleteUserProfileDirectlyAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DatingBot.Application.Common.Result<AdminModerationActionResult>.Failure("Not found"));
        _adminService.Setup(a => a.GetAdminProfileByGenderAsync(Gender.Female, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, 0, 0));

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(adminUser, query);

        handled.Should().BeTrue();
        _botClient.Verify(b => b.SendRequest(
            It.Is<AnswerCallbackQueryRequest>(r => r.CallbackQueryId == "q_err_del" && r.Text != null && r.Text.Contains("Ошибка удаления анкеты") && r.ShowAlert == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAdminCallbackQueryAsync_BanInSearch_WhenFails_ShouldShowLocalizedAlert()
    {
        var adminUser = new User { TelegramId = 123, Language = AppLanguage.Russian };
        var targetUserId = Guid.NewGuid();
        var query = new CallbackQuery
        {
            Id = "q_err_ban",
            Data = $"adm_s_ban:{targetUserId}:male:0",
            From = new Telegram.Bot.Types.User { Id = 123 }
        };

        _adminService.Setup(a => a.IsAdmin(123)).Returns(true);
        _adminService.Setup(a => a.BanUserDirectlyAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DatingBot.Application.Common.Result<AdminModerationActionResult>.Failure("Not found"));
        _adminService.Setup(a => a.GetAdminProfileByGenderAsync(Gender.Male, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, 0, 0));

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(adminUser, query);

        handled.Should().BeTrue();
        _botClient.Verify(b => b.SendRequest(
            It.Is<AnswerCallbackQueryRequest>(r => r.CallbackQueryId == "q_err_ban" && r.Text != null && r.Text.Contains("Ошибка блокировки пользователя") && r.ShowAlert == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAdminCallbackQueryAsync_ReportBan_WhenAlreadyProcessed_ShouldShowAlert()
    {
        var adminUser = new User { TelegramId = 123, Language = AppLanguage.Russian };
        var reportId = Guid.NewGuid();
        var query = new CallbackQuery
        {
            Id = "q_rep_err",
            Data = $"adm_rep_ban:{reportId}:0",
            From = new Telegram.Bot.Types.User { Id = 123 }
        };

        _adminService.Setup(a => a.IsAdmin(123)).Returns(true);
        _moderationService.Setup(m => m.BanUserByReportAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DatingBot.Application.Common.Result<ModerationActionResult>.Failure("Already resolved"));
        _adminService.Setup(a => a.GetPendingReportsAsync(0, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _adminService.Setup(a => a.GetPendingReportsCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(adminUser, query);

        handled.Should().BeTrue();
        _botClient.Verify(b => b.SendRequest(
            It.Is<AnswerCallbackQueryRequest>(r => r.CallbackQueryId == "q_rep_err" && r.Text != null && r.Text.Contains("уже была обработана") && r.ShowAlert == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
