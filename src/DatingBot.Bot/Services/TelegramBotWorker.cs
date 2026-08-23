using DatingBot.Bot.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DatingBot.Bot.Services;

/// <summary>
/// Фоновый сервис непрерывного и самовосстанавливающегося получения обновлений Telegram-бота (Long Polling).
/// При любых сетевых сбоях, недоступности API или падениях потока автоматически возобновляет работу.
/// </summary>
public class TelegramBotWorker(
    ITelegramBotClient botClient,
    IServiceScopeFactory scopeFactory,
    IBotLifecycleCoordinator lifecycle,
    ILogger<TelegramBotWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TelegramBotWorker инициализирован. Ожидание готовности базы данных...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Ожидаем завершения миграций и сидирования БД перед стартом обработки входящих запросов
                await lifecycle.WaitForDatabaseReadyAsync(stoppingToken);

                try
                {
                    var me = await botClient.GetMe(stoppingToken);
                    logger.LogInformation("Успешное подключение к Telegram Bot API: @{BotUsername} (ID: {BotId})", me.Username, me.Id);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Предупреждение при проверке GetMe. Переход к сессии опроса...");
                }

                lifecycle.SetTelegramPollingActive(true);

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery, UpdateType.PreCheckoutQuery],
                    DropPendingUpdates = true
                };

                using var pollingCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                logger.LogInformation("Запуск сессии опроса Telegram Long Polling...");

                await botClient.ReceiveAsync(
                    updateHandler: (bot, update, ct) => ProcessUpdateSafeAsync(update, ct),
                    errorHandler: (bot, ex, ct) =>
                    {
                        HandleTelegramPollingError(ex, ct);
                        return Task.CompletedTask;
                    },
                    receiverOptions: receiverOptions,
                    cancellationToken: pollingCts.Token
                );
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("TelegramBotWorker остановлен по сигналу отмены приложения.");
                break;
            }
            catch (Exception ex)
            {
                lifecycle.RecordTelegramRestart(ex);
                var restartCount = lifecycle.TelegramRestartCount;
                var delaySeconds = Math.Min(Math.Pow(2, Math.Min(restartCount, 4)), 20);
                var delay = TimeSpan.FromSeconds(delaySeconds);

                logger.LogError(ex,
                    "Критический сбой сессии опроса Telegram API (Перезапуск #{Count}). Автоматическое самовосстановление через {Delay} сек...",
                    restartCount, delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
            finally
            {
                lifecycle.SetTelegramPollingActive(false);
            }
        }
    }

    /// <summary>
    /// Безопасная обработка входящего апдейта с изоляцией исключений в рамках отдельного скоупа.
    /// Сбой при обработке сообщения одного пользователя гарантированно не прерывает работу бота для других.
    /// </summary>
    public async Task ProcessUpdateSafeAsync(Update update, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var router = scope.ServiceProvider.GetRequiredService<TelegramUpdateRouter>();
            await router.RouteUpdateAsync(update, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Корректная отмена при завершении приложения
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Необработанная ошибка при обработке Telegram апдейта [ID: {UpdateId}, Тип: {UpdateType}]. Бот продолжает работу.",
                update.Id, update.Type);
        }
    }

    /// <summary>
    /// Обработчик ошибок Telegram API и сетевого уровня Long Polling.
    /// </summary>
    public void HandleTelegramPollingError(Exception exception, CancellationToken cancellationToken = default)
    {
        if (exception is ApiRequestException apiEx)
        {
            logger.LogError("Ошибка Telegram API: Код {ErrorCode} — {Message}",
                apiEx.ErrorCode, apiEx.Message);
        }
        else
        {
            logger.LogError(exception, "Сетевая ошибка или сбой опроса Telegram API: {Message}",
                exception.Message);
        }

        lifecycle.RecordTelegramRestart(exception);
    }
}
