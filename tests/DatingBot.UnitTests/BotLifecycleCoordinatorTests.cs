using DatingBot.Bot.Services;
using FluentAssertions;
using Xunit;

namespace DatingBot.UnitTests;

public class BotLifecycleCoordinatorTests
{
    [Fact]
    public void InitialState_HasDatabaseNotReadyAndTelegramNotActive()
    {
        // Arrange & Act
        var coordinator = new BotLifecycleCoordinator();

        // Assert
        coordinator.IsDatabaseReady.Should().BeFalse();
        coordinator.IsTelegramPollingActive.Should().BeFalse();
        coordinator.DatabaseRetryCount.Should().Be(0);
        coordinator.TelegramRestartCount.Should().Be(0);
        coordinator.LastDatabaseError.Should().BeNull();
        coordinator.LastTelegramError.Should().BeNull();
        coordinator.DatabaseReadyAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkDatabaseReady_SetsIsDatabaseReadyTrueAndRecordsTimestamp()
    {
        // Arrange
        var coordinator = new BotLifecycleCoordinator();

        // Act
        coordinator.MarkDatabaseReady();

        // Assert
        coordinator.IsDatabaseReady.Should().BeTrue();
        coordinator.DatabaseReadyAtUtc.Should().NotBeNull();
        coordinator.DatabaseReadyAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task WaitForDatabaseReadyAsync_WhenMarkedReady_CompletesSuccessfully()
    {
        // Arrange
        var coordinator = new BotLifecycleCoordinator();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        var waitTask = coordinator.WaitForDatabaseReadyAsync(cts.Token);
        waitTask.IsCompleted.Should().BeFalse();

        coordinator.MarkDatabaseReady();
        await waitTask;

        // Assert
        waitTask.IsCompletedSuccessfully.Should().BeTrue();
        coordinator.IsDatabaseReady.Should().BeTrue();
    }

    [Fact]
    public async Task WaitForDatabaseReadyAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var coordinator = new BotLifecycleCoordinator();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        var act = async () => await coordinator.WaitForDatabaseReadyAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void RecordDatabaseError_IncrementsRetryCountAndSavesErrorMessage()
    {
        // Arrange
        var coordinator = new BotLifecycleCoordinator();
        var exception1 = new InvalidOperationException("Connection timeout to SQL server");
        var exception2 = new InvalidOperationException("Cannot open database");

        // Act
        coordinator.RecordDatabaseError(exception1);
        coordinator.RecordDatabaseError(exception2);

        // Assert
        coordinator.DatabaseRetryCount.Should().Be(2);
        coordinator.LastDatabaseError.Should().Be("Cannot open database");
        coordinator.IsDatabaseReady.Should().BeFalse();
    }

    [Fact]
    public void SetTelegramPollingActive_UpdatesActiveStatus()
    {
        // Arrange
        var coordinator = new BotLifecycleCoordinator();

        // Act
        coordinator.SetTelegramPollingActive(true);

        // Assert
        coordinator.IsTelegramPollingActive.Should().BeTrue();

        // Act
        coordinator.SetTelegramPollingActive(false);

        // Assert
        coordinator.IsTelegramPollingActive.Should().BeFalse();
    }

    [Fact]
    public void RecordTelegramRestart_IncrementsRestartCountAndSavesErrorMessage()
    {
        // Arrange
        var coordinator = new BotLifecycleCoordinator();
        var exception = new HttpRequestException("Telegram API 502 Bad Gateway");

        // Act
        coordinator.RecordTelegramRestart(exception);

        // Assert
        coordinator.TelegramRestartCount.Should().Be(1);
        coordinator.LastTelegramError.Should().Be("Telegram API 502 Bad Gateway");
        coordinator.IsTelegramPollingActive.Should().BeFalse();
    }
}
