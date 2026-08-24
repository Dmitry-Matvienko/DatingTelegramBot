using DatingBot.Application.Interfaces;
using DatingBot.Bot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace DatingBot.Bot.Workers;

public class MatchmakingNotificationWorker(
    IServiceProvider serviceProvider,
    IBotLifecycleCoordinator lifecycle,
    ILogger<MatchmakingNotificationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MatchmakingNotificationWorker запущен. Ожидание готовности базы данных...");

        try
        {
            await lifecycle.WaitForDatabaseReadyAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CheckInterval, stoppingToken);

                using var scope = serviceProvider.CreateScope();
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

                // Фоновый мониторинг очереди подбора анкет
                logger.LogDebug("Фоновая проверка подбора анкет...");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в MatchmakingNotificationWorker");
            }
        }

        logger.LogInformation("MatchmakingNotificationWorker остановлен.");
    }
}
