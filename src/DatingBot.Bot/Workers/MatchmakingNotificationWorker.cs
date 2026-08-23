using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace DatingBot.Bot.Workers;

public class MatchmakingNotificationWorker(
    IServiceProvider serviceProvider,
    ILogger<MatchmakingNotificationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MatchmakingNotificationWorker запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CheckInterval, stoppingToken);

                using var scope = serviceProvider.CreateScope();
                var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

                // Фоновое напоминание пользователям о возможности возобновить поиск через 24ч
                logger.LogDebug("Проверка пользователей для 24-часового напоминания о поиске...");
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
