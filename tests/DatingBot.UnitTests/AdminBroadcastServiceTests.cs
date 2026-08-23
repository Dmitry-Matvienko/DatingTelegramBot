using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Services;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;

namespace DatingBot.UnitTests;

public class AdminBroadcastServiceTests
{
    private readonly Mock<ITelegramBotClient> _botClient = new();
    private readonly Mock<IAdminService> _adminService = new();
    private readonly Mock<ILogger<AdminBroadcastService>> _logger = new();

    private readonly AdminBroadcastService _sut;

    public AdminBroadcastServiceTests()
    {
        _sut = new AdminBroadcastService(
            _botClient.Object,
            _adminService.Object,
            _logger.Object
        );
    }

    [Fact]
    public void SessionManagement_ShouldStoreAndClearPerAdmin()
    {
        var session = _sut.GetOrCreateSession(123);
        session.Text = "Реклама!";
        session.Filter = new AdminBroadcastFilterDto(TargetGender: Gender.Female);

        var retrieved = _sut.GetOrCreateSession(123);
        retrieved.Text.Should().Be("Реклама!");
        retrieved.Filter.TargetGender.Should().Be(Gender.Female);

        _sut.ClearSession(123);

        var newSession = _sut.GetOrCreateSession(123);
        newSession.Text.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteBroadcastAsync_ShouldDeliverMessagesAndReturnStats()
    {
        var session = new AdminBroadcastSession
        {
            Text = "Скидки 50% на свидания!",
            ButtonText = "Перейти",
            ButtonUrl = "https://example.com",
            Filter = new AdminBroadcastFilterDto()
        };

        _adminService.Setup(a => a.GetBroadcastRecipientTelegramIdsAsync(session.Filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync([101, 102]);

        _botClient.Setup(b => b.SendRequest(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message());

        var result = await _sut.ExecuteBroadcastAsync(session);

        result.TotalTargets.Should().Be(2);
        result.DeliveredCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteBroadcastAsync_ShouldIsolateErrorsWhenDeliveryFails()
    {
        var session = new AdminBroadcastSession
        {
            Text = "Рекламный пост",
            Filter = new AdminBroadcastFilterDto()
        };

        _adminService.Setup(a => a.GetBroadcastRecipientTelegramIdsAsync(session.Filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync([201, 202, 203]);

        var callCount = 0;
        _botClient.Setup(b => b.SendRequest(
            It.IsAny<SendMessageRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new InvalidOperationException("Bot was blocked by the user");
                }
                return new Message();
            });

        var result = await _sut.ExecuteBroadcastAsync(session);

        result.TotalTargets.Should().Be(3);
        result.DeliveredCount.Should().Be(2);
        result.FailedCount.Should().Be(1);
    }
}
