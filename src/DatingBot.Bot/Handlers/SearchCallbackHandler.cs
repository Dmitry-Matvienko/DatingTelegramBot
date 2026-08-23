using DatingBot.Application.Interfaces;
using DatingBot.Bot.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using DbUser = DatingBot.Domain.Entities.User;

namespace DatingBot.Bot.Handlers;

public class SearchCallbackHandler(
    ITelegramBotClient botClient,
    ISearchService searchService,
    SearchPromptService searchPromptService,
    ProfilePromptService profilePromptService,
    RegistrationPromptService registrationPromptService,
    ILocalizationService loc)
{
    public async Task<bool> HandleSearchCallbackQueryAsync(DbUser user, CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var data = callbackQuery.Data ?? string.Empty;
        var chatId = callbackQuery.Message?.Chat.Id ?? user.TelegramId;
        var messageId = callbackQuery.Message?.MessageId;
        var lang = user.Language;

        // 1. Нажатие кнопки "👀 Показать кто оценил"
        if (data.StartsWith("view_rater:"))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            var rawId = data["view_rater:".Length..];
            if (Guid.TryParse(rawId, out var ratingId))
            {
                var incoming = await searchService.GetIncomingRatingByIdAsync(user.TelegramId, ratingId, cancellationToken);
                if (incoming is not null)
                {
                    await searchPromptService.SendRaterCardAsync(chatId, incoming.RaterProfile, incoming.ScoreReceived, cancellationToken);
                    return true;
                }
            }

            // Если конкретная оценка уже обработана, пробуем следующую входящую
            var nextIncoming = await searchService.GetNextIncomingRatingAsync(user.TelegramId, cancellationToken);
            if (nextIncoming is not null)
            {
                await searchPromptService.SendRaterCardAsync(chatId, nextIncoming.RaterProfile, nextIncoming.ScoreReceived, cancellationToken);
            }
            else
            {
                await ShowNextCandidateOrIncomingAsync(chatId, user.TelegramId, cancellationToken);
            }
            return true;
        }

        // 2. Нажатие кнопки "🚨 Пожаловаться" -> меню причин
        if (data.StartsWith("report:"))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            var rawId = data["report:".Length..];
            if (Guid.TryParse(rawId, out var candidateProfileId))
            {
                await searchPromptService.SendReportReasonsPromptAsync(chatId, candidateProfileId, cancellationToken);
            }
            return true;
        }

        // 3. Отмена жалобы -> удаляем меню выбора причин
        if (data.StartsWith("report_cancel:"))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            if (messageId.HasValue)
            {
                await registrationPromptService.DeleteMessageSafeAsync(chatId, messageId.Value, cancellationToken);
            }
            return true;
        }

        // 4. Выбор причины жалобы
        if (data.StartsWith("report_reason:"))
        {
            var parts = data.Split(':');
            if (parts.Length == 3 && Guid.TryParse(parts[1], out var candidateProfileId) && int.TryParse(parts[2], out var reasonInt))
            {
                var reason = (ReportReason)reasonInt;

                if (messageId.HasValue)
                {
                    await registrationPromptService.DeleteMessageSafeAsync(chatId, messageId.Value, cancellationToken);
                }

                if (reason == ReportReason.Other)
                {
                    // Переводим в состояние ввода текста жалобы
                    await searchService.SetReportingStateAsync(user.TelegramId, candidateProfileId, cancellationToken);
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
                    await searchPromptService.SendCustomReasonPromptAsync(chatId, cancellationToken);
                    return true;
                }

                var reportResult = await searchService.ReportCandidateAsync(user.TelegramId, candidateProfileId, reason, null, cancellationToken);

                if (reportResult.IsSuccess)
                {
                    await searchPromptService.SendReportToAdminsAsync(reportResult.Value!, cancellationToken);
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, loc.Get(lang, "Report_SentAdmin"), showAlert: true, cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
                }

                // Переход к следующей анкете
                await ShowNextCandidateOrIncomingAsync(chatId, user.TelegramId, cancellationToken);
            }
            return true;
        }

        // 5. Сброс истории для города ("Начать поиск заново")
        if (data == "search:reset_city")
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, loc.Get(lang, "City_SearchReset"), cancellationToken: cancellationToken);
            await searchService.ResetHistoryForCityAsync(user.TelegramId, cancellationToken);
            await ShowNextCandidateOrIncomingAsync(chatId, user.TelegramId, cancellationToken);
            return true;
        }

        // 6. Возврат в главное меню из поиска
        if (data == "search:main_menu")
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            await searchService.ClearCurrentCandidateAsync(user.TelegramId, cancellationToken);
            await profilePromptService.SendMainMenuGreetingAsync(chatId, messageId, cancellationToken);
            return true;
        }

        return false;
    }

    public async Task HandleRatingFromReplyKeyboardAsync(long chatId, DbUser user, int score, CancellationToken cancellationToken = default)
    {
        if (!user.CurrentCandidateProfileId.HasValue)
        {
            await ShowNextCandidateOrIncomingAsync(chatId, user.TelegramId, cancellationToken);
            return;
        }

        var candidateProfileId = user.CurrentCandidateProfileId.Value;
        var rateResult = await searchService.RateCandidateAsync(user.TelegramId, candidateProfileId, score, cancellationToken);

        if (rateResult.IsSuccess && rateResult.Value is not null)
        {
            var result = rateResult.Value;
            if (result.IsMutualMatch && result.CandidateProfile is not null && result.RaterProfile is not null)
            {
                // Взаимная симпатия! Отправляем уведомления обеим сторонам
                await searchPromptService.SendMutualMatchNotificationAsync(
                    user.TelegramId,
                    result.CandidateProfile,
                    score,
                    result.OriginalScore,
                    cancellationToken
                );

                await searchPromptService.SendMutualMatchNotificationAsync(
                    result.ToTelegramId,
                    result.RaterProfile,
                    result.OriginalScore,
                    score,
                    cancellationToken
                );
            }
            else if (score >= 6)
            {
                // Оценка >= 6 -> уведомление с кнопкой "Показать кто оценил"
                await searchPromptService.SendHighRatingNotificationAsync(
                    result.ToTelegramId,
                    result.RatingId,
                    score,
                    cancellationToken
                );
            }
        }

        // Показываем следующего оценившего или следующего кандидата в поиске
        await ShowNextCandidateOrIncomingAsync(chatId, user.TelegramId, cancellationToken);
    }

    public async Task ShowNextCandidateOrIncomingAsync(long chatId, long telegramId, CancellationToken cancellationToken = default)
    {
        // Сначала проверяем, есть ли входящие оценки, которые пользователь еще не оценил
        var incoming = await searchService.GetNextIncomingRatingAsync(telegramId, cancellationToken);
        if (incoming is not null)
        {
            await searchPromptService.SendRaterCardAsync(chatId, incoming.RaterProfile, incoming.ScoreReceived, cancellationToken);
            return;
        }

        // Каскадный поиск кандидата
        var nextMatch = await searchService.GetNextMatchCandidateAsync(telegramId, cancellationToken);
        if (nextMatch is not null)
        {
            await searchPromptService.SendMatchCandidateCardAsync(chatId, nextMatch, cancellationToken);
        }
        else
        {
            await searchPromptService.SendNoCandidatesMessageAsync(chatId, cancellationToken);
        }
    }
}
