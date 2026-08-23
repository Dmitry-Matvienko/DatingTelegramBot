using DatingBot.Bot.Handlers;
using DatingBot.Bot.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace DatingBot.UnitTests;

public class SelfHealingTelegramBotWorkerTests
{
    private readonly Mock<ITelegramBotClient> _botClientMock = new();
    private readonly Mock<ILogger<TelegramBotWorker>> _loggerMock = new();
    private readonly Mock<IBotLifecycleCoordinator> _lifecycleMock = new();

    public SelfHealingTelegramBotWorkerTests()
    {
        _lifecycleMock.Setup(l => l.WaitForDatabaseReadyAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task ProcessUpdateSafeAsync_WhenRouterThrowsException_CatchesAndLogsWithoutCrashing()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddScoped(_ =>
            {
                var r = new Mock<TelegramUpdateRouter>(
                    Mock.Of<IServiceProvider>(),
                    Mock.Of<ILogger<TelegramUpdateRouter>>(),
                    Mock.Of<DatingBot.Application.Interfaces.IUserRepository>(),
                    Mock.Of<DatingBot.Application.Interfaces.ILocalizationService>()
                );
                r.Setup(x => x.RouteUpdateAsync(It.IsAny<Update>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Fatal unexpected database error during update routing"));
                return r.Object;
            })
            .BuildServiceProvider();

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProvider);
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var worker = new TelegramBotWorker(
            _botClientMock.Object,
            scopeFactoryMock.Object,
            _lifecycleMock.Object,
            _loggerMock.Object
        );

        var sampleUpdate = new Update
        {
            Id = 12345,
            Message = new Message
            {
                Id = 1,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = 999999, Type = ChatType.Private },
                From = new User { Id = 999999, FirstName = "TestUser", IsBot = false },
                Text = "/start"
            }
        };

        // Act
        var act = async () => await worker.ProcessUpdateSafeAsync(sampleUpdate, CancellationToken.None);

        // Assert - must not throw exception!
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void HandleTelegramError_WhenApiOrNetworkErrorOccurs_LogsAndRecordsToCoordinator()
    {
        // Arrange
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var coordinator = new BotLifecycleCoordinator();

        var worker = new TelegramBotWorker(
            _botClientMock.Object,
            scopeFactoryMock.Object,
            coordinator,
            _loggerMock.Object
        );

        var exception = new HttpRequestException("Connection closed by remote host");

        // Act
        worker.HandleTelegramPollingError(exception, CancellationToken.None);

        // Assert
        coordinator.LastTelegramError.Should().Be("Connection closed by remote host");
        coordinator.TelegramRestartCount.Should().Be(1);
    }
}
