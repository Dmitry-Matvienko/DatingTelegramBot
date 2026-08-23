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

public class TelegramBotWorker(
    ITelegramBotClient botClient,
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramBotWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var me = await botClient.GetMe(stoppingToken);
            logger.LogInformation("Запущен Telegram бот: @{BotUsername} (ID: {BotId})", me.Username, me.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось подключиться к Telegram при старте. Проверьте BotToken в appsettings.Local.json или переменных окружения.");
        }

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery, UpdateType.PreCheckoutQuery],
            DropPendingUpdates = true
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        // Держим воркер активным до отмены токена
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var router = scope.ServiceProvider.GetRequiredService<TelegramUpdateRouter>();
        await router.RouteUpdateAsync(update, cancellationToken);
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        if (exception is ApiRequestException apiEx)
        {
            logger.LogError("Ошибка Telegram API: [{ErrorCode}] {Message}", apiEx.ErrorCode, apiEx.Message);
        }
        else
        {
            logger.LogError(exception, "Необработанная ошибка в TelegramBotWorker");
        }

        return Task.CompletedTask;
    }
}
