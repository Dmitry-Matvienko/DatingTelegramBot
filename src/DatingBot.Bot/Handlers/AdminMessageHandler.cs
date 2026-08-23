using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Bot.Services;
using DatingBot.Domain.Enums;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DbUser = DatingBot.Domain.Entities.User;

namespace DatingBot.Bot.Handlers;

public class AdminMessageHandler(
    ITelegramBotClient botClient,
    IAdminService adminService,
    AdminPromptService adminPromptService,
    AdminBroadcastService adminBroadcastService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILocalizationService loc)
{
    public async Task<bool> HandleAdminMessageAsync(DbUser user, Message message, CancellationToken cancellationToken = default)
    {
        var text = message.Text?.Trim();
        var lang = user.Language;
        var chatId = message.Chat.Id;

        // 1. Ввод города для расчета статистики рекламодателю
        if (user.State == UserState.Admin_Stats_WaitingForCity)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                await botClient.SendMessage(chatId, loc.Get(lang, "Admin_Stats_CityPrompt"), parseMode: ParseMode.Html, cancellationToken: cancellationToken);
                return true;
            }

            var cityStats = await adminService.GetCityStatsAsync(text, cancellationToken);
            if (cityStats is null)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "Admin_Stats_CityNotFound", text),
                    parseMode: ParseMode.Html,
                    replyMarkup: AdminKeyboards.GetBackToStatsKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                return true;
            }

            await adminPromptService.SendCityStatsAsync(chatId, cityStats, cancellationToken);
            return true;
        }

        // 2. Ввод города для таргетированной рассылки
        if (user.State == UserState.Admin_Broadcasting_WaitingForCity)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                var session = adminBroadcastService.GetOrCreateSession(user.TelegramId);
                session.Filter = session.Filter with { City = text };
            }

            user.State = UserState.Admin_Broadcasting_WaitingForContent;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Broadcast_Content_Prompt"),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
            return true;
        }

        // 3. Ввод контента рекламного сообщения (текст / фото)
        if (user.State == UserState.Admin_Broadcasting_WaitingForContent)
        {
            var session = adminBroadcastService.GetOrCreateSession(user.TelegramId);

            if (message.Photo is { Length: > 0 } photos)
            {
                session.PhotoFileId = photos.Last().FileId;
                session.Text = message.Caption?.Trim() ?? string.Empty;
            }
            else if (!string.IsNullOrWhiteSpace(text))
            {
                session.PhotoFileId = null;
                session.Text = text;
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "Admin_Broadcast_Content_Prompt"),
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                return true;
            }

            user.State = UserState.Admin_Broadcasting_WaitingForButton;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Broadcast_Button_Prompt"),
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboards.GetAdminBroadcastSkipButtonKeyboard(lang),
                cancellationToken: cancellationToken
            );
            return true;
        }

        // 4. Ввод инлайн-кнопки со ссылкой для рассылки
        if (user.State == UserState.Admin_Broadcasting_WaitingForButton)
        {
            var session = adminBroadcastService.GetOrCreateSession(user.TelegramId);

            if (string.IsNullOrWhiteSpace(text))
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "Admin_Broadcast_Button_Invalid"),
                    parseMode: ParseMode.Html,
                    replyMarkup: AdminKeyboards.GetAdminBroadcastSkipButtonKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                return true;
            }

            var parts = text.Split('|', 2);
            if (parts.Length == 2 &&
                !string.IsNullOrWhiteSpace(parts[0]) &&
                !string.IsNullOrWhiteSpace(parts[1]) &&
                Uri.TryCreate(parts[1].Trim(), UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                session.ButtonText = parts[0].Trim();
                session.ButtonUrl = parts[1].Trim();

                user.State = UserState.Admin_Panel;
                userRepository.Update(user);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                session.CalculatedReach = await adminService.GetBroadcastAudienceCountAsync(session.Filter, cancellationToken);
                var preview = new AdminBroadcastPreviewDto(session.Text, session.PhotoFileId, session.ButtonText, session.ButtonUrl, session.CalculatedReach, session.Filter);
                await adminPromptService.SendBroadcastPreviewAsync(chatId, preview, cancellationToken);
                return true;
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Broadcast_Button_Invalid"),
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboards.GetAdminBroadcastSkipButtonKeyboard(lang),
                cancellationToken: cancellationToken
            );
            return true;
        }

        return false;
    }
}
