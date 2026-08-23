using System.Collections.Concurrent;
using System.Diagnostics;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Services;

public class AdminBroadcastSession
{
    public AdminBroadcastFilterDto Filter { get; set; } = new();
    public string? Text { get; set; }
    public string? PhotoFileId { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
    public int CalculatedReach { get; set; }
}

public class AdminBroadcastService(
    ITelegramBotClient botClient,
    IAdminService adminService,
    ILogger<AdminBroadcastService> logger)
{
    private readonly ConcurrentDictionary<long, AdminBroadcastSession> _sessions = new();

    public AdminBroadcastSession GetOrCreateSession(long adminTelegramId)
    {
        return _sessions.GetOrAdd(adminTelegramId, _ => new AdminBroadcastSession());
    }

    public void ClearSession(long adminTelegramId)
    {
        _sessions.TryRemove(adminTelegramId, out _);
    }

    public async Task<AdminBroadcastResultDto> ExecuteBroadcastAsync(
        AdminBroadcastSession session,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var recipientIds = await adminService.GetBroadcastRecipientTelegramIdsAsync(session.Filter, cancellationToken);
        var total = recipientIds.Count;
        var delivered = 0;
        var failed = 0;

        InlineKeyboardMarkup? replyMarkup = null;
        if (!string.IsNullOrWhiteSpace(session.ButtonText) && !string.IsNullOrWhiteSpace(session.ButtonUrl))
        {
            replyMarkup = new InlineKeyboardMarkup([
                [InlineKeyboardButton.WithUrl(session.ButtonText, session.ButtonUrl)]
            ]);
        }

        const int batchSize = 25;
        const int delayMs = 1000;

        for (var i = 0; i < recipientIds.Count; i += batchSize)
        {
            var batch = recipientIds.Skip(i).Take(batchSize);

            foreach (var recipientId in batch)
            {
                try
                {
                    var photoSent = false;
                    if (!string.IsNullOrEmpty(session.PhotoFileId) && (session.Text == null || session.Text.Length <= 1024))
                    {
                        try
                        {
                            await botClient.SendPhoto(
                                chatId: recipientId,
                                photo: InputFile.FromFileId(session.PhotoFileId),
                                caption: session.Text,
                                parseMode: ParseMode.Html,
                                replyMarkup: replyMarkup,
                                cancellationToken: cancellationToken
                            );
                            photoSent = true;
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug("Не удалось доставить фото рассылки пользователю {TelegramId}: {ErrorMessage}, пробуем текстом", recipientId, ex.Message);
                        }
                    }

                    if (!photoSent)
                    {
                        await botClient.SendMessage(
                            chatId: recipientId,
                            text: session.Text ?? string.Empty,
                            parseMode: ParseMode.Html,
                            replyMarkup: replyMarkup,
                            cancellationToken: cancellationToken
                        );
                    }

                    delivered++;
                }
                catch (Exception ex)
                {
                    failed++;
                    logger.LogDebug(ex, "Не удалось доставить рассылку пользователю {TelegramId}", recipientId);
                }
            }

            if (i + batchSize < recipientIds.Count)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }

        sw.Stop();
        return new AdminBroadcastResultDto(total, delivered, failed, sw.Elapsed);
    }
}
