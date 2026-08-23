using DatingBot.Application.Interfaces;
using DatingBot.Bot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Workers;

public class InactivityNotificationWorker(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IBotLifecycleCoordinator lifecycle,
    ILogger<InactivityNotificationWorker> logger) : BackgroundService
{
    private int InactivityReminderDays =>
        int.TryParse(configuration["BotConfiguration:InactivityReminderDays"], out var days) && days > 0 ? days : 3;

    private int InactivityCheckIntervalMinutes =>
        int.TryParse(configuration["BotConfiguration:InactivityCheckIntervalMinutes"], out var mins) && mins > 0 ? mins : 60;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("InactivityNotificationWorker запущен. Ожидание готовности базы данных...");

        try
        {
            await lifecycle.WaitForDatabaseReadyAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("InactivityNotificationWorker активирован (периодичность неактивности: {InactivityReminderDays} дн., интервал проверки: {Interval} мин).",
            InactivityReminderDays, InactivityCheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var checkInterval = TimeSpan.FromMinutes(InactivityCheckIntervalMinutes);
                await Task.Delay(checkInterval, stoppingToken);

                await ProcessInactiveUsersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка в главном цикле InactivityNotificationWorker");
            }
        }

        logger.LogInformation("InactivityNotificationWorker остановлен.");
    }

    public async Task<int> ProcessInactiveUsersAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var inactivityReminderService = scope.ServiceProvider.GetRequiredService<IInactivityReminderService>();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var loc = scope.ServiceProvider.GetRequiredService<ILocalizationService>();

        var reminderDays = InactivityReminderDays;
        var inactiveUsers = await inactivityReminderService.GetUsersForInactivityReminderAsync(reminderDays, limit: 100, cancellationToken);

        if (inactiveUsers.Count == 0)
        {
            logger.LogDebug("Нет пользователей для отправки напоминаний о неактивности.");
            return 0;
        }

        logger.LogInformation("Найдено {Count} неактивных пользователей для отправки напоминания.", inactiveUsers.Count);

        var sentCount = 0;
        var now = DateTime.UtcNow;

        foreach (var user in inactiveUsers)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var randomKey = inactivityReminderService.GetRandomInactivityReminderKey();
                var messageText = loc.Get(user.Language, randomKey);
                var buttonText = loc.Get(user.Language, "Btn_Inactivity_StartSearch");

                var inlineKeyboard = new InlineKeyboardMarkup([
                    [InlineKeyboardButton.WithCallbackData(buttonText, "inactivity_search")]
                ]);

                await botClient.SendMessage(
                    chatId: user.TelegramId,
                    text: messageText,
                    parseMode: ParseMode.Html,
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken
                );

                await inactivityReminderService.MarkReminderSentAsync(user.Id, now, cancellationToken);
                sentCount++;
                logger.LogInformation("Отправлено напоминание о неактивности пользователю {TelegramId} (Шаблон: {Key})", user.TelegramId, randomKey);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось отправить напоминание пользователю {TelegramId}: {Message}. Помечаем отправку для предотвращения бесконечных повторов.",
                    user.TelegramId, ex.Message);

                // При сбое (например, пользователь заблокировал бота) обновляем отметку отправки, чтобы не повторять каждую итерацию
                await inactivityReminderService.MarkReminderSentAsync(user.Id, now, cancellationToken);
            }

            // Небольшая пауза между отправками
            await Task.Delay(50, cancellationToken);
        }

        return sentCount;
    }
}
