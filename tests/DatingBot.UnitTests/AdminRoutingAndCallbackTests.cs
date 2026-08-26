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

        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection().Build();

        _callbackHandler = new AdminCallbackHandler(
            _botClient.Object,
            _adminService.Object,
            _moderationService.Object,
            _promptService,
            _broadcastService,
            _userRepo.Object,
            _unitOfWork.Object,
            _loc,
            config,
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

    [Fact]
    public async Task HandleAdminCallbackQueryAsync_Revenue_ShouldOpenRevenueMenu()
    {
        var adminUser = new User { TelegramId = 123, Language = AppLanguage.Russian, State = UserState.Admin_Panel };
        var query = new CallbackQuery
        {
            Id = "q_rev",
            Data = "adm_panel:revenue",
            From = new Telegram.Bot.Types.User { Id = 123 }
        };

        _adminService.Setup(a => a.IsAdmin(123)).Returns(true);

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(adminUser, query);

        handled.Should().BeTrue();
        adminUser.State.Should().Be(UserState.Admin_Revenue);
    }

    [Fact]
    public async Task HandleAdminCallbackQueryAsync_RevenueBalance_ShouldSendBalanceReport()
    {
        var adminUser = new User { TelegramId = 123, Language = AppLanguage.Russian, State = UserState.Admin_Revenue };
        var query = new CallbackQuery
        {
            Id = "q_bal",
            Data = "adm_rev:balance",
            From = new Telegram.Bot.Types.User { Id = 123 }
        };

        _adminService.Setup(a => a.IsAdmin(123)).Returns(true);
        _adminService.Setup(a => a.GetRevenueStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminRevenueStatsDto(500, 5, 100, 300, 500, []));

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(adminUser, query);

        handled.Should().BeTrue();
        _adminService.Verify(a => a.GetRevenueStatsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAdminCallbackQueryAsync_RevenueHistory_ShouldSendHistoryReport()
    {
        var adminUser = new User { TelegramId = 123, Language = AppLanguage.Russian, State = UserState.Admin_Revenue };
        var query = new CallbackQuery
        {
            Id = "q_hist",
            Data = "adm_rev:history",
            From = new Telegram.Bot.Types.User { Id = 123 }
        };

        _adminService.Setup(a => a.IsAdmin(123)).Returns(true);
        _adminService.Setup(a => a.GetRecentTransactionsAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(adminUser, query);

        handled.Should().BeTrue();
        _adminService.Verify(a => a.GetRecentTransactionsAsync(20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAdminCandidateCardAsync_WhenCandidateHasHtmlCharacters_ShouldProperlyHtmlEncodeFields()
    {
        var chatId = 12345L;
        _userRepo.Setup(r => r.GetByTelegramIdAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = chatId, Language = AppLanguage.Russian });

        var candidate = new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: 7661135900L,
            Username: "danger_user",
            Gender: Gender.Male,
            TargetGender: TargetGender.Female,
            Name: "Влад <3",
            Age: 20,
            City: "Краснодар <Юг> & Край",
            Height: 182,
            PhotoFileId: "photo_123",
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "Bio with <b>tags</b> & <script>alert(1)</script>",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.None,
            SearchMinAge: null,
            SearchMaxAge: null,
            RatingCount: 0,
            AverageRating: 0.0,
            CityId: null,
            AiVector: null,
            Greeting: "Всем привет <3! Ищу rock & roll"
        );

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 100 });

        await _promptService.SendAdminCandidateCardAsync(chatId, candidate, Gender.Male, 5, 25, 6);

        _botClient.Verify(b => b.SendRequest(
            It.Is<SendPhotoRequest>(r =>
                r.Caption != null &&
                r.Caption.Contains("Влад &lt;3") &&
                r.Caption.Contains("Краснодар &lt;Юг&gt; &amp; Край") &&
                r.Caption.Contains("Bio with &lt;b&gt;tags&lt;/b&gt; &amp; &lt;script&gt;alert(1)&lt;/script&gt;") &&
                r.Caption.Contains("Всем привет &lt;3! Ищу rock &amp; roll") &&
                !r.Caption.Contains("<3") &&
                !r.Caption.Contains("<script>")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAdminCandidateCardAsync_WhenPhotoFailsAndHtmlSendMessageFails_ShouldFallbackToPlainText()
    {
        var chatId = 12345L;
        _userRepo.Setup(r => r.GetByTelegramIdAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = chatId, Language = AppLanguage.Russian });

        var candidate = new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: 7661135900L,
            Username: null,
            Gender: Gender.Male,
            TargetGender: TargetGender.Female,
            Name: "Влад",
            Age: 20,
            City: "Краснодар",
            Height: 182,
            PhotoFileId: "expired_photo_id",
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "Bio",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.None,
            SearchMinAge: null,
            SearchMaxAge: null,
            RatingCount: 0,
            AverageRating: 0.0,
            CityId: null,
            AiVector: null,
            Greeting: "Привет"
        );

        // Photo fails (e.g. invalid photo file ID)
        _botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Telegram.Bot.Exceptions.ApiRequestException("Bad Request: wrong file identifier", 400));

        // HTML SendMessage fails (e.g. entity parse error)
        _botClient.Setup(b => b.SendRequest(It.Is<SendMessageRequest>(r => r.ParseMode == ParseMode.Html), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Telegram.Bot.Exceptions.ApiRequestException("Bad Request: can't parse entities", 400));

        // Plain text fallback succeeds
        _botClient.Setup(b => b.SendRequest(It.Is<SendMessageRequest>(r => r.ParseMode == ParseMode.None), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 102 });

        await _promptService.SendAdminCandidateCardAsync(chatId, candidate, Gender.Male, 5, 25, 6);

        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.ChatId.Identifier == chatId && r.ParseMode == ParseMode.None),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendPendingReportCardAsync_WhenReportHasHtmlCharacters_ShouldProperlyHtmlEncodeFields()
    {
        var chatId = 12345L;
        _userRepo.Setup(r => r.GetByTelegramIdAsync(chatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { TelegramId = chatId, Language = AppLanguage.Russian });

        var reportedProfile = new UserProfileDto(
            Id: Guid.NewGuid(),
            TelegramId: 7661135900L,
            Username: "reported_user",
            Gender: Gender.Male,
            TargetGender: TargetGender.Female,
            Name: "Иван <3",
            Age: 22,
            City: "Сочи <Центр>",
            Height: 180,
            PhotoFileId: "photo_rep",
            DatingTarget: DatingTarget.Relationship,
            AiDescription: "Bio with <tag>",
            SelectedInterests: [],
            IsCompleted: true,
            AgeFilters: AgeCategoryFilter.None,
            SearchMinAge: null,
            SearchMaxAge: null,
            RatingCount: 0,
            AverageRating: 0.0,
            CityId: null,
            AiVector: null,
            Greeting: "Привет <3"
        );

        var report = new AdminPendingReportDto(
            ReportId: Guid.NewGuid(),
            ReporterUserId: Guid.NewGuid(),
            ReporterTelegramId: 111L,
            ReporterUsername: "reporter_user",
            ReporterFirstName: "Заявитель <1>",
            ReporterLanguage: AppLanguage.Russian,
            ReportedUserId: reportedProfile.Id,
            ReportedProfile: reportedProfile,
            Reason: ReportReason.InappropriateContent,
            Details: "Жалоба: нарушает <правила> & спамит",
            CreatedAt: DateTime.UtcNow
        );

        _botClient.Setup(b => b.SendRequest(It.IsAny<SendPhotoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Id = 100 });

        await _promptService.SendPendingReportCardAsync(chatId, report, 1, 10, 2);

        _botClient.Verify(b => b.SendRequest(
            It.Is<SendPhotoRequest>(r =>
                r.Caption != null &&
                r.Caption.Contains("Заявитель &lt;1&gt;") &&
                r.Caption.Contains("Иван &lt;3") &&
                r.Caption.Contains("Сочи &lt;Центр&gt;") &&
                r.Caption.Contains("Жалоба: нарушает &lt;правила&gt; &amp; спамит") &&
                !r.Caption.Contains("<1>") &&
                !r.Caption.Contains("<3") &&
                !r.Caption.Contains("<правила>")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAdminCallbackQueryAsync_NextInSearch_WhenAdminServiceThrows_ShouldCatchAndShowAlert()
    {
        var adminUser = new User { TelegramId = 123, Language = AppLanguage.Russian };
        var query = new CallbackQuery
        {
            Id = "q_next_err",
            Data = "adm_s_next:male:5",
            From = new Telegram.Bot.Types.User { Id = 123 }
        };

        _adminService.Setup(a => a.IsAdmin(123)).Returns(true);
        _adminService.Setup(a => a.GetAdminProfileByGenderAsync(Gender.Male, 5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection lost"));

        var handled = await _callbackHandler.HandleAdminCallbackQueryAsync(adminUser, query);

        handled.Should().BeTrue();
        _botClient.Verify(b => b.SendRequest(
            It.Is<SendMessageRequest>(r => r.Text.Contains("Ошибка при отображении анкеты") || r.Text.Contains("Ошибка")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
