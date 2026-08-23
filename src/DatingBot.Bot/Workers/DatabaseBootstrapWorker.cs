using DatingBot.Application.Interfaces;
using DatingBot.Bot.Services;
using DatingBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DatingBot.Bot.Workers;

/// <summary>
/// Фоновый сервис самовосстанавливающейся и неблокирующей инициализации базы данных.
/// При сбоях сети/СУБД при старте выполняет повторные попытки с экспоненциальным backoff,
/// не блокируя запуск Kestrel веб-сервера и Keep-Alive пингов.
/// </summary>
public class DatabaseBootstrapWorker(
    IServiceScopeFactory scopeFactory,
    IBotLifecycleCoordinator lifecycle,
    ILogger<DatabaseBootstrapWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Запущен воркер самовосстанавливающейся инициализации БД (DatabaseBootstrapWorker).");

        while (!stoppingToken.IsCancellationRequested && !lifecycle.IsDatabaseReady)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (dbContext.Database.IsRelational())
                {
                    logger.LogInformation("Применение миграций базы данных...");
                    await dbContext.Database.MigrateAsync(stoppingToken);
                }

                logger.LogInformation("Проверка и сидирование базы данных городов...");
                var seeder = scope.ServiceProvider.GetRequiredService<ICityDatabaseSeeder>();
                await seeder.SeedAsync(stoppingToken);

                lifecycle.MarkDatabaseReady();
                logger.LogInformation("База данных успешно инициализирована, миграции и сидирование завершены.");
                break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Инициализация базы данных отменена при остановке приложения.");
                break;
            }
            catch (Exception ex)
            {
                lifecycle.RecordDatabaseError(ex);
                var retryCount = lifecycle.DatabaseRetryCount;
                var delaySeconds = Math.Min(Math.Pow(2, Math.Min(retryCount, 5)), 30);
                var delay = TimeSpan.FromSeconds(delaySeconds);

                logger.LogWarning(ex,
                    "Ошибка подключения/миграции базы данных (попытка #{Attempt}). Повтор через {Delay} сек...",
                    retryCount, delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
