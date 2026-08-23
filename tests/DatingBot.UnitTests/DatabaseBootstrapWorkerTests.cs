using DatingBot.Application.Interfaces;
using DatingBot.Bot.Services;
using DatingBot.Bot.Workers;
using DatingBot.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DatingBot.UnitTests;

public class DatabaseBootstrapWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSeederSucceeds_MarksDatabaseReady()
    {
        // Arrange
        var coordinator = new BotLifecycleCoordinator();
        var mockSeeder = new Mock<ICityDatabaseSeeder>();
        mockSeeder.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("BootstrapTestDb_Success_" + Guid.NewGuid()));
        services.AddScoped(_ => mockSeeder.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(() => serviceProvider.CreateScope());

        var worker = new DatabaseBootstrapWorker(
            scopeFactoryMock.Object,
            coordinator,
            NullLogger<DatabaseBootstrapWorker>.Instance
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        await worker.StartAsync(cts.Token);
        await coordinator.WaitForDatabaseReadyAsync(cts.Token);
        await worker.StopAsync(cts.Token);

        // Assert
        coordinator.IsDatabaseReady.Should().BeTrue();
        coordinator.DatabaseRetryCount.Should().Be(0);
        coordinator.LastDatabaseError.Should().BeNull();
        mockSeeder.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSeederFailsInitiallyThenSucceeds_RetriesAndMarksDatabaseReady()
    {
        // Arrange
        var coordinator = new BotLifecycleCoordinator();
        var mockSeeder = new Mock<ICityDatabaseSeeder>();
        var attempt = 0;

        mockSeeder.Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attempt++;
                if (attempt == 1)
                {
                    throw new TimeoutException("Database connection timeout during cloud cold start");
                }
                return Task.CompletedTask;
            });

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("BootstrapTestDb_Retry_" + Guid.NewGuid()));
        services.AddScoped(_ => mockSeeder.Object);

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(() => serviceProvider.CreateScope());

        var worker = new DatabaseBootstrapWorker(
            scopeFactoryMock.Object,
            coordinator,
            NullLogger<DatabaseBootstrapWorker>.Instance
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        await worker.StartAsync(cts.Token);
        await coordinator.WaitForDatabaseReadyAsync(cts.Token);
        await worker.StopAsync(cts.Token);

        // Assert
        coordinator.IsDatabaseReady.Should().BeTrue();
        coordinator.DatabaseRetryCount.Should().Be(1);
        coordinator.LastDatabaseError.Should().Contain("Database connection timeout");
        attempt.Should().Be(2);
    }
}
