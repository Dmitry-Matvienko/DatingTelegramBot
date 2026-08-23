using DatingBot.Application.Interfaces;
using DatingBot.Bot.Workers;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;
using User = DatingBot.Domain.Entities.User;

namespace DatingBot.UnitTests;

public class InactivityNotificationWorkerTests
{
    private readonly Mock<IInactivityReminderService> _inactivityReminderServiceMock = new();
    private readonly Mock<ITelegramBotClient> _botClientMock = new();
    private readonly Mock<ILocalizationService> _locMock = new();
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public InactivityNotificationWorkerTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["BotConfiguration:InactivityReminderDays"] = "3",
            ["BotConfiguration:InactivityCheckIntervalMinutes"] = "60"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddScoped(_ => _inactivityReminderServiceMock.Object);
        services.AddScoped(_ => _botClientMock.Object);
        services.AddScoped(_ => _locMock.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ProcessInactiveUsersAsync_ShouldSendNotificationAndMarkSent()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 555777,
            Language = AppLanguage.Russian,
            State = UserState.Active
        };

        _inactivityReminderServiceMock
            .Setup(s => s.GetUsersForInactivityReminderAsync(3, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);

        _inactivityReminderServiceMock
            .Setup(s => s.GetRandomInactivityReminderKey())
            .Returns("Notification_Inactivity_1");

        _locMock
            .Setup(l => l.Get(AppLanguage.Russian, "Notification_Inactivity_1"))
            .Returns("Заманчивое напоминание 1");

        _locMock
            .Setup(l => l.Get(AppLanguage.Russian, "Btn_Inactivity_StartSearch"))
            .Returns("🔍 Начать поиск");

        _botClientMock
            .Setup(b => b.SendRequest(
                It.Is<Telegram.Bot.Requests.SendMessageRequest>(r => r.ChatId.Identifier == 555777 && r.Text == "Заманчивое напоминание 1"),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new Message());

        var worker = new InactivityNotificationWorker(
            _serviceProvider,
            _configuration,
            NullLogger<InactivityNotificationWorker>.Instance
        );

        var sentCount = await worker.ProcessInactiveUsersAsync();

        sentCount.Should().Be(1);
        _inactivityReminderServiceMock.Verify(s => s.MarkReminderSentAsync(user.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessInactiveUsersAsync_WhenNoUsers_ShouldReturnZero()
    {
        _inactivityReminderServiceMock
            .Setup(s => s.GetUsersForInactivityReminderAsync(3, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var worker = new InactivityNotificationWorker(
            _serviceProvider,
            _configuration,
            NullLogger<InactivityNotificationWorker>.Instance
        );

        var sentCount = await worker.ProcessInactiveUsersAsync();

        sentCount.Should().Be(0);
        _botClientMock.VerifyNoOtherCalls();
    }
}
