using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Bot.Services;
using DatingBot.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;

namespace DatingBot.Bot.Handlers;

public class TelegramUpdateRouter(
    ITelegramBotClient botClient,
    IRegistrationService registrationService,
    ISearchService searchService,
    IAdminService adminService,
    IModerationService moderationService,
    ILocalizationService loc,
    IConfiguration configuration,
    RegistrationMessageHandler registrationMessageHandler,
    RegistrationCallbackHandler registrationCallbackHandler,
    RegistrationPromptService registrationPromptService,
    ProfilePromptService profilePromptService,
    ProfileEditMessageHandler profileEditMessageHandler,
    ProfileEditCallbackHandler profileEditCallbackHandler,
    SearchPromptService searchPromptService,
    SearchCallbackHandler searchCallbackHandler,
    AdminPromptService adminPromptService,
    AdminCallbackHandler adminCallbackHandler,
    AdminMessageHandler adminMessageHandler,
    ILogger<TelegramUpdateRouter> logger)
{
    public async Task RouteUpdateAsync(Update update, CancellationToken cancellationToken = default)
    {
        try
        {
            if (update.Type == UpdateType.PreCheckoutQuery && update.PreCheckoutQuery is { } preCheckoutQuery)
            {
                var payload = preCheckoutQuery.InvoicePayload ?? string.Empty;
                if (payload.StartsWith("unban:") || payload.StartsWith("unban_"))
                {
                    await botClient.AnswerPreCheckoutQuery(
                        preCheckoutQueryId: preCheckoutQuery.Id,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await botClient.AnswerPreCheckoutQuery(
                        preCheckoutQueryId: preCheckoutQuery.Id,
                        errorMessage: "Invalid invoice",
                        cancellationToken: cancellationToken
                    );
                }
                return;
            }

            if (update.Type == UpdateType.Message && update.Message is { } message)
            {
                var telegramId = message.From?.Id ?? message.Chat.Id;
                var username = message.From?.Username;
                var firstName = message.From?.FirstName;

                var user = await registrationService.GetOrCreateUserAsync(telegramId, username, firstName, cancellationToken);
                var prevBotMsgId = user.LastBotMessageId;
                var text = message.Text?.Trim();
                var lang = user.Language;
                var isAdmin = adminService.IsAdmin(telegramId);

                // Обработка успешного платежа через Telegram Stars
                if (message.SuccessfulPayment is { } successfulPayment)
                {
                    var payload = successfulPayment.InvoicePayload ?? string.Empty;

                    await adminService.RecordSuccessfulPaymentAsync(
                        telegramId: telegramId,
                        amount: (int)successfulPayment.TotalAmount,
                        currency: successfulPayment.Currency,
                        type: payload.StartsWith("unban") ? PaymentType.Unban : PaymentType.Other,
                        payload: payload,
                        telegramPaymentChargeId: successfulPayment.TelegramPaymentChargeId,
                        providerPaymentChargeId: successfulPayment.ProviderPaymentChargeId,
                        cancellationToken: cancellationToken
                    );

                    if (payload.StartsWith("unban:") || payload.StartsWith("unban_"))
                    {
                        var idPart = payload.Contains(':') ? payload.Split(':')[1] : payload["unban_".Length..];
                        if (Guid.TryParse(idPart, out var targetUserId))
                        {
                            var unbanResult = await moderationService.UnbanUserAsync(targetUserId, cancellationToken);
                            if (unbanResult.IsSuccess && unbanResult.Value is not null)
                            {
                                var userLang = unbanResult.Value.Language;
                                await botClient.SendMessage(
                                    chatId: message.Chat.Id,
                                    text: loc.Get(userLang, "Notification_UnbanSuccessful"),
                                    parseMode: ParseMode.Html,
                                    replyMarkup: MainMenuKeyboards.GetMainMenuReplyKeyboard(userLang),
                                    cancellationToken: cancellationToken
                                );
                                return;
                            }
                        }
                    }
                }

                // Блокировка заблокированных пользователей
                if (user.State == UserState.Banned)
                {
                    var unbanPrice = GetUnbanPriceStars();
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: loc.Get(lang, "Account_Banned"),
                        parseMode: ParseMode.Html,
                        replyMarkup: PaymentKeyboards.GetUnbanKeyboard(lang, loc, unbanPrice),
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // 1. Команда /start
                if (text?.Equals("/start", StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (isAdmin)
                    {
                        user.State = UserState.Active;
                        await adminPromptService.SendAdminWelcomeAsync(message.Chat.Id, prevBotMsgId, cancellationToken);
                        return;
                    }

                    if (user.State == UserState.Active || user.State == UserState.Searching)
                    {
                        var profile = await registrationService.GetProfileDtoAsync(telegramId, cancellationToken);
                        if (profile is not null)
                        {
                            await profilePromptService.SendProfileCardAsync(message.Chat.Id, profile, prevBotMsgId, cancellationToken);
                            return;
                        }
                    }

                    // Если новый пользователь — сразу спрашиваем язык интерфейса
                    if (user.State == UserState.None || user.State == UserState.Registration_SelectingLanguage)
                    {
                        await registrationService.ResetRegistrationAsync(telegramId, cancellationToken);
                        user.State = UserState.Registration_SelectingLanguage;
                        await registrationPromptService.SendPromptForStateAsync(message.Chat.Id, UserState.Registration_SelectingLanguage, prevBotMsgId, null, cancellationToken);
                        return;
                    }

                    await registrationPromptService.SendPromptForStateAsync(message.Chat.Id, user.State, prevBotMsgId, null, cancellationToken);
                    return;
                }

                // 2. Команда /reset
                if (text?.Equals("/reset", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await registrationPromptService.DeleteMessageSafeAsync(message.Chat.Id, message.MessageId, cancellationToken);

                    if (isAdmin)
                    {
                        user.State = UserState.Active;
                        await adminPromptService.SendAdminWelcomeAsync(message.Chat.Id, prevBotMsgId, cancellationToken);
                        return;
                    }

                    await registrationService.ResetRegistrationAsync(telegramId, cancellationToken);
                    await registrationPromptService.SendPromptForStateAsync(message.Chat.Id, UserState.Registration_SelectingLanguage, prevBotMsgId, null, cancellationToken);
                    return;
                }

                // 3. Выбор языка: кнопка "🌐 Язык" или /language
                if (IsLanguageButton(text) || text?.Equals("/language", StringComparison.OrdinalIgnoreCase) == true || text?.Equals("/lang", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await registrationPromptService.DeleteMessageSafeAsync(message.Chat.Id, message.MessageId, cancellationToken);
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: loc.Get(lang, "LanguagePrompt"),
                        parseMode: ParseMode.Html,
                        replyMarkup: LanguageKeyboards.GetLanguageSelectionKeyboard("edit_lang"),
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // 4. Обработка сообщений администратора в спец. состояниях (поиск по городу, рассылка)
                if (isAdmin)
                {
                    var isHandledByAdminMsg = await adminMessageHandler.HandleAdminMessageAsync(user, message, cancellationToken);
                    if (isHandledByAdminMsg)
                    {
                        return;
                    }
                }

                // 5. Обработка ввода текстовой причины жалобы ("❓ Другое")
                if (user.State == UserState.Reporting_WaitingForDetails)
                {
                    if (text == loc.Get(lang, "Btn_Cancel") || text?.Equals("❌ Отмена", StringComparison.OrdinalIgnoreCase) == true || text?.Equals("❌ Cancel", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        await searchCallbackHandler.ShowNextCandidateOrIncomingAsync(message.Chat.Id, user.TelegramId, cancellationToken);
                        return;
                    }

                    if (user.CurrentCandidateProfileId.HasValue && !string.IsNullOrWhiteSpace(text))
                    {
                        var reportResult = await searchService.ReportCandidateAsync(
                            user.TelegramId,
                            user.CurrentCandidateProfileId.Value,
                            ReportReason.Other,
                            text,
                            cancellationToken
                        );

                        if (reportResult.IsSuccess)
                        {
                            await searchPromptService.SendReportToAdminsAsync(reportResult.Value!, cancellationToken);
                            await botClient.SendMessage(message.Chat.Id, loc.Get(lang, "Report_SentAdmin"), cancellationToken: cancellationToken);
                        }
                    }

                    await searchCallbackHandler.ShowNextCandidateOrIncomingAsync(message.Chat.Id, user.TelegramId, cancellationToken);
                    return;
                }

                // 6. Меню: "🔍 Поиск" или /search
                if (IsSearchButton(text) || text?.Equals("/search", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await registrationPromptService.DeleteMessageSafeAsync(message.Chat.Id, message.MessageId, cancellationToken);

                    if (isAdmin)
                    {
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: loc.Get(lang, "Admin_Search_Gender_Prompt"),
                            parseMode: ParseMode.Html,
                            replyMarkup: AdminKeyboards.GetAdminSearchGenderKeyboard(lang),
                            cancellationToken: cancellationToken
                        );
                        return;
                    }

                    var profile = await registrationService.GetProfileDtoAsync(telegramId, cancellationToken);
                    if (profile is null || !profile.IsCompleted)
                    {
                        await registrationPromptService.SendPromptForStateAsync(message.Chat.Id, user.State, prevBotMsgId, loc.Get(lang, "Search_MustCompleteProfile"), cancellationToken);
                        return;
                    }

                    await searchCallbackHandler.ShowNextCandidateOrIncomingAsync(message.Chat.Id, telegramId, cancellationToken);
                    return;
                }

                // 7. Обработка кнопок в режиме поиска (Reply Keyboard)
                if (user.State == UserState.Searching || user.State == UserState.Searching_ViewingIncoming)
                {
                    if (IsReportButton(text))
                    {
                        if (user.CurrentCandidateProfileId.HasValue)
                        {
                            await searchPromptService.SendReportReasonsPromptAsync(message.Chat.Id, user.CurrentCandidateProfileId.Value, cancellationToken);
                        }
                        return;
                    }

                    if (IsSearchAgainButton(text))
                    {
                        await searchService.ResetHistoryForCityAsync(user.TelegramId, cancellationToken);
                        await botClient.SendMessage(
                            chatId: message.Chat.Id,
                            text: loc.Get(lang, "City_SearchReset"),
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken
                        );
                        await searchCallbackHandler.ShowNextCandidateOrIncomingAsync(message.Chat.Id, user.TelegramId, cancellationToken);
                        return;
                    }

                    if (TryParseScore(text, out var score))
                    {
                        await searchCallbackHandler.HandleRatingFromReplyKeyboardAsync(message.Chat.Id, user, score, cancellationToken);
                        return;
                    }
                }

                // 8. Меню: "👤 Мой профиль" или /profile
                if (IsProfileButton(text) || text?.Equals("/profile", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await registrationPromptService.DeleteMessageSafeAsync(message.Chat.Id, message.MessageId, cancellationToken);

                    if (isAdmin)
                    {
                        await adminPromptService.SendAdminPanelAsync(message.Chat.Id, prevBotMsgId, cancellationToken);
                        return;
                    }

                    var profile = await registrationService.GetProfileDtoAsync(telegramId, cancellationToken);
                    if (profile is not null && profile.IsCompleted)
                    {
                        await profilePromptService.SendProfileCardAsync(message.Chat.Id, profile, prevBotMsgId, cancellationToken);
                    }
                    else
                    {
                        await registrationPromptService.SendPromptForStateAsync(message.Chat.Id, user.State, prevBotMsgId, loc.Get(lang, "Search_MustCompleteProfile"), cancellationToken);
                    }
                    return;
                }

                // 9. Меню: "🏠 Главное меню" или /menu
                if (IsMainMenuButton(text) || text?.Equals("/menu", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await registrationPromptService.DeleteMessageSafeAsync(message.Chat.Id, message.MessageId, cancellationToken);

                    if (isAdmin)
                    {
                        await adminPromptService.SendAdminWelcomeAsync(message.Chat.Id, prevBotMsgId, cancellationToken);
                        return;
                    }

                    await searchService.ClearCurrentCandidateAsync(user.TelegramId, cancellationToken);
                    await profilePromptService.SendMainMenuGreetingAsync(message.Chat.Id, prevBotMsgId, cancellationToken);
                    return;
                }

                // 10. Если пользователь в режиме редактирования профиля
                var isEditingHandled = await profileEditMessageHandler.HandleEditMessageAsync(user, message, cancellationToken);
                if (isEditingHandled)
                {
                    return;
                }

                // 11. Обычная ветка регистрации
                await registrationMessageHandler.HandleMessageAsync(user, message, cancellationToken);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery is { } callbackQuery)
            {
                var telegramId = callbackQuery.From.Id;
                var user = await registrationService.GetOrCreateUserAsync(telegramId, callbackQuery.From.Username, callbackQuery.From.FirstName, cancellationToken);

                // 1. Проверяем обработчик панели администратора и модерации
                var isAdminCallback = await adminCallbackHandler.HandleAdminCallbackQueryAsync(user, callbackQuery, cancellationToken);
                if (isAdminCallback)
                {
                    return;
                }

                // Обработка кнопки разбана за звёзды
                if (callbackQuery.Data == "pay_unban")
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

                    var unbanPrice = GetUnbanPriceStars();
                    var priceLabel = string.Format(loc.Get(user.Language, "Payment_Unban_PriceLabel"), unbanPrice);

                    await botClient.SendInvoice(
                        chatId: callbackQuery.Message?.Chat.Id ?? callbackQuery.From.Id,
                        title: loc.Get(user.Language, "Payment_Unban_Title"),
                        description: loc.Get(user.Language, "Payment_Unban_Description"),
                        payload: $"unban:{user.Id}",
                        currency: "XTR",
                        prices: [new LabeledPrice(priceLabel, unbanPrice)],
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // Блокировка заблокированных пользователей для обычных действий
                if (user.State == UserState.Banned)
                {
                    await botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        loc.Get(user.Language, "Account_Banned"),
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                    return;
                }

                // Обработка кнопки "🔍 Начать поиск" из напоминания о неактивности
                if (callbackQuery.Data == "inactivity_search")
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

                    var profile = await registrationService.GetProfileDtoAsync(telegramId, cancellationToken);
                    if (profile is null || !profile.IsCompleted)
                    {
                        await registrationPromptService.SendPromptForStateAsync(
                            callbackQuery.Message?.Chat.Id ?? telegramId,
                            user.State,
                            user.LastBotMessageId,
                            loc.Get(user.Language, "Search_MustCompleteProfile"),
                            cancellationToken
                        );
                        return;
                    }

                    user.State = UserState.Searching;
                    await searchCallbackHandler.ShowNextCandidateOrIncomingAsync(
                        callbackQuery.Message?.Chat.Id ?? telegramId,
                        telegramId,
                        cancellationToken
                    );
                    return;
                }

                // 2. Проверяем обработчик поиска и оценок
                var isSearchCallback = await searchCallbackHandler.HandleSearchCallbackQueryAsync(user, callbackQuery, cancellationToken);
                if (isSearchCallback)
                {
                    return;
                }

                // 3. Проверяем обработчик редактирования профиля
                var isEditCallback = await profileEditCallbackHandler.HandleEditCallbackQueryAsync(user, callbackQuery, cancellationToken);
                if (isEditCallback)
                {
                    return;
                }

                // 4. Обработчик регистрации
                await registrationCallbackHandler.HandleCallbackQueryAsync(user, callbackQuery, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке Telegram Update: {UpdateId}", update.Id);
        }
    }

    private static bool IsSearchButton(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.StartsWith("🔍") || text.Contains("Поиск") || text.Contains("Шукати") || text.Contains("Search") || text.Contains("खोजें") || text.Contains("Procurar") || text.Contains("Cari");
    }

    private static bool IsProfileButton(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.StartsWith("👤") || text.Contains("профиль") || text.Contains("профіль") || text.Contains("Profile") || text.Contains("प्रोफ़ाइल") || text.Contains("Perfil");
    }

    private static bool IsLanguageButton(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.StartsWith("🌐") || text.Contains("Язык") || text.Contains("Мова") || text.Contains("Language") || text.Contains("भाषा") || text.Contains("Idioma") || text.Contains("Bahasa");
    }

    private static bool IsMainMenuButton(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.StartsWith("🏠") || text.Contains("Главное меню") || text.Contains("Головне меню") || text.Contains("Main Menu") || text.Contains("मुख्य मेनू") || text.Contains("Menu Principal") || text.Contains("Menu Utama");
    }

    private static bool IsReportButton(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.StartsWith("🚨") || text.Contains("Пожаловаться") || text.Contains("Поскаржитися") || text.Contains("Report") || text.Contains("शिकायत") || text.Contains("Denunciar") || text.Contains("Laporkan");
    }

    private static bool IsSearchAgainButton(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.StartsWith("🔄") || text.Contains("Искать заново") || text.Contains("Шукати заново") || text.Contains("Search again") || text.Contains("फिर से खोजें") || text.Contains("Buscar novamente") || text.Contains("Cari lagi");
    }

    private static bool TryParseScore(string? text, out int score)
    {
        score = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var clean = text.Replace("️⃣", "").Replace("🔟", "10").Trim();
        if (int.TryParse(clean, out score) && score >= 1 && score <= 10)
        {
            return true;
        }

        return false;
    }

    private int GetUnbanPriceStars() =>
        int.TryParse(configuration["BotConfiguration:UnbanPriceStars"], out var price) && price > 0 ? price : 100;
}
