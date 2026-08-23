using DatingBot.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DbUser = DatingBot.Domain.Entities.User;

namespace DatingBot.Bot.Handlers;

public class AdminModerationCallbackHandler(
    ITelegramBotClient botClient,
    IModerationService moderationService,
    ILocalizationService loc,
    IConfiguration configuration,
    ILogger<AdminModerationCallbackHandler> logger)
{
    public async Task<bool> HandleAdminCallbackQueryAsync(DbUser user, CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var data = callbackQuery.Data ?? string.Empty;
        if (!data.StartsWith("adm_ban:") && !data.StartsWith("adm_del:") && !data.StartsWith("adm_ign:"))
        {
            return false;
        }

        var adminTelegramId = callbackQuery.From.Id;
        var adminIds = configuration.GetSection("BotConfiguration:AdminIds").Get<List<long>>() ?? [];
        if (!adminIds.Contains(adminTelegramId))
        {
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                loc.Get(user.Language, "Admin_Alert_NoAccess"),
                showAlert: true,
                cancellationToken: cancellationToken
            );
            return true;
        }

        var parts = data.Split(':');
        if (parts.Length != 2 || !Guid.TryParse(parts[1], out var reportId))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            return true;
        }

        var adminTag = string.IsNullOrEmpty(callbackQuery.From.Username)
            ? $"<b>{callbackQuery.From.FirstName}</b>"
            : $"@{callbackQuery.From.Username}";

        if (data.StartsWith("adm_ban:"))
        {
            var result = await moderationService.BanUserByReportAsync(reportId, cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    loc.Get(user.Language, "Admin_Decision_UserBanned"),
                    showAlert: true,
                    cancellationToken: cancellationToken
                );

                await UpdateAdminMessageAsync(callbackQuery.Message, $"{loc.Get(user.Language, "Admin_Decision_UserBanned")} ({adminTag})", cancellationToken);

                // Уведомление заявителю
                await NotifyReporterSafeAsync(result.Value.ReporterTelegramId, result.Value.ReporterLanguage, cancellationToken);

                // Уведомление нарушителю
                await NotifyViolatorBannedSafeAsync(result.Value.ReportedTelegramId, result.Value.ReportedLanguage, cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    loc.Get(user.Language, "Admin_Alert_AlreadyProcessed"),
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
            }
            return true;
        }

        if (data.StartsWith("adm_del:"))
        {
            var result = await moderationService.DeleteProfileByReportAsync(reportId, cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    loc.Get(user.Language, "Admin_Decision_ProfileDeleted"),
                    showAlert: true,
                    cancellationToken: cancellationToken
                );

                await UpdateAdminMessageAsync(callbackQuery.Message, $"{loc.Get(user.Language, "Admin_Decision_ProfileDeleted")} ({adminTag})", cancellationToken);

                // Уведомление заявителю
                await NotifyReporterSafeAsync(result.Value.ReporterTelegramId, result.Value.ReporterLanguage, cancellationToken);

                // Уведомление нарушителю
                await NotifyViolatorProfileDeletedSafeAsync(result.Value.ReportedTelegramId, result.Value.ReportedLanguage, cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    loc.Get(user.Language, "Admin_Alert_AlreadyProcessed"),
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
            }
            return true;
        }

        if (data.StartsWith("adm_ign:"))
        {
            var result = await moderationService.IgnoreReportAsync(reportId, cancellationToken);
            if (result.IsSuccess)
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    loc.Get(user.Language, "Admin_Decision_ReportIgnored"),
                    cancellationToken: cancellationToken
                );

                await UpdateAdminMessageAsync(callbackQuery.Message, $"{loc.Get(user.Language, "Admin_Decision_ReportIgnored")} ({adminTag})", cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    loc.Get(user.Language, "Admin_Alert_AlreadyProcessed"),
                    showAlert: true,
                    cancellationToken: cancellationToken
                );
            }
            return true;
        }

        return false;
    }

    private async Task UpdateAdminMessageAsync(Message? message, string statusText, CancellationToken cancellationToken)
    {
        if (message is { } msg)
        {
            try
            {
                var newText = $"{msg.Text}\n\n{statusText}";
                await botClient.EditMessageText(
                    chatId: msg.Chat.Id,
                    messageId: msg.MessageId,
                    text: newText,
                    parseMode: ParseMode.Html,
                    replyMarkup: null,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось обновить сообщение администратора {MessageId}", msg.MessageId);
            }
        }
    }

    private async Task NotifyReporterSafeAsync(long reporterTelegramId, Domain.Enums.AppLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var message = loc.Get(language, "Notification_ReportResolved");
            await botClient.SendMessage(
                chatId: reporterTelegramId,
                text: message,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning("Не удалось отправить уведомление заявителю {ReporterTelegramId}: {ErrorMessage}", reporterTelegramId, ex.Message);
        }
    }

    private async Task NotifyViolatorBannedSafeAsync(long violatorTelegramId, Domain.Enums.AppLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var message = loc.Get(language, "Notification_ViolatorBanned");
            await botClient.SendMessage(
                chatId: violatorTelegramId,
                text: message,
                parseMode: ParseMode.Html,
                replyMarkup: DatingBot.Bot.Keyboards.PaymentKeyboards.GetUnbanKeyboard(language, loc),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning("Не удалось отправить уведомление о бане нарушителю {ViolatorTelegramId}: {ErrorMessage}", violatorTelegramId, ex.Message);
        }
    }

    private async Task NotifyViolatorProfileDeletedSafeAsync(long violatorTelegramId, Domain.Enums.AppLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var message = loc.Get(language, "Notification_ViolatorProfileDeleted");
            await botClient.SendMessage(
                chatId: violatorTelegramId,
                text: message,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning("Не удалось отправить уведомление об удалении профиля нарушителю {ViolatorTelegramId}: {ErrorMessage}", violatorTelegramId, ex.Message);
        }
    }
}
