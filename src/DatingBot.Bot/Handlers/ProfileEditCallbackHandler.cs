using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Bot.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using DbUser = DatingBot.Domain.Entities.User;

namespace DatingBot.Bot.Handlers;

public class ProfileEditCallbackHandler(
    ITelegramBotClient botClient,
    IProfileEditingService editingService,
    IRegistrationService registrationService,
    ICityRepository cityRepository,
    ProfilePromptService profilePromptService,
    ILocalizationService loc)
{
    public async Task<bool> HandleEditCallbackQueryAsync(DbUser user, CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var data = callbackQuery.Data ?? string.Empty;
        var chatId = callbackQuery.Message?.Chat.Id ?? user.TelegramId;
        var messageId = callbackQuery.Message?.MessageId;
        var lang = user.Language;

        if (data.StartsWith("edit_city_confirm:"))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            var rawId = data["edit_city_confirm:".Length..];
            if (int.TryParse(rawId, out var cityId))
            {
                var city = await cityRepository.GetByIdAsync(cityId, cancellationToken);
                if (city is not null)
                {
                    await editingService.UpdateCityAsync(user.TelegramId, city.Name, cancellationToken);
                    await SendUpdatedProfileAsync(chatId, user.TelegramId, messageId, cancellationToken);
                    return true;
                }
            }

            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_City, messageId, loc.Get(lang, "City_TypeManually"), cancellationToken);
            return true;
        }

        if (data == "edit_city_retry")
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_City, messageId, loc.Get(lang, "City_TypeManually"), cancellationToken);
            return true;
        }

        if (!data.StartsWith("edit"))
        {
            return false;
        }

        // Подтверждаем callback
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        if (data == "edit:cancel")
        {
            await editingService.CancelEditingAsync(user.TelegramId, cancellationToken);
            await SendUpdatedProfileAsync(chatId, user.TelegramId, messageId, cancellationToken);
            return true;
        }

        if (data == "edit:language")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_Language, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Language, messageId, null, cancellationToken);
            return true;
        }

        if (data.StartsWith("edit_lang_"))
        {
            var rawLang = data["edit_lang_".Length..];
            if (int.TryParse(rawLang, out var langInt) && Enum.IsDefined(typeof(AppLanguage), langInt))
            {
                var language = (AppLanguage)langInt;
                await editingService.UpdateLanguageAsync(user.TelegramId, language, cancellationToken);
                await profilePromptService.SendMainMenuGreetingAsync(chatId, messageId, cancellationToken);
            }
            return true;
        }

        if (data == "edit:main_menu")
        {
            await editingService.CancelEditingAsync(user.TelegramId, cancellationToken);
            await profilePromptService.SendMainMenuGreetingAsync(chatId, messageId, cancellationToken);
            return true;
        }

        if (data == "edit:name")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_Name, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Name, messageId, null, cancellationToken);
            return true;
        }

        if (data == "edit:age")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_Age, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Age, messageId, null, cancellationToken);
            return true;
        }

        if (data == "edit:city")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_City, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_City, messageId, null, cancellationToken);
            return true;
        }

        if (data == "edit:height")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_Height, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Height, messageId, null, cancellationToken);
            return true;
        }

        if (data == "edit:height_remove")
        {
            await editingService.UpdateHeightAsync(user.TelegramId, null, cancellationToken);
            await SendUpdatedProfileAsync(chatId, user.TelegramId, messageId, cancellationToken);
            return true;
        }

        if (data == "edit:photo")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_Photo, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Photo, messageId, null, cancellationToken);
            return true;
        }

        // 1. Изменение Пола -> переход сразу к "Кого вы ищете?"
        if (data == "edit:gender")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_Gender, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Gender, messageId, null, cancellationToken);
            return true;
        }

        if (data.StartsWith("edit_gender:"))
        {
            var rawValue = data["edit_gender:".Length..];
            if (int.TryParse(rawValue, out var genderInt) && Enum.IsDefined(typeof(Gender), genderInt))
            {
                await editingService.UpdateGenderAsync(user.TelegramId, (Gender)genderInt, cancellationToken);
                // Сразу же переходим к вопросу "Кого вы ищете?"
                await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_TargetGender, cancellationToken);
                await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_TargetGender, messageId, null, cancellationToken);
            }
            return true;
        }

        if (data.StartsWith("edit_target_gender:"))
        {
            var rawValue = data["edit_target_gender:".Length..];
            if (int.TryParse(rawValue, out var tgInt) && Enum.IsDefined(typeof(TargetGender), tgInt))
            {
                await editingService.UpdateTargetGenderAsync(user.TelegramId, (TargetGender)tgInt, cancellationToken);
                await SendUpdatedProfileAsync(chatId, user.TelegramId, messageId, cancellationToken);
            }
            return true;
        }

        // 2. Параметры поиска по возрасту
        if (data == "edit:search_params")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_SearchAgeCategories, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_SearchAgeCategories, messageId, null, cancellationToken);
            return true;
        }

        if (data.StartsWith("edit_age_cat:"))
        {
            var rawValue = data["edit_age_cat:".Length..];
            if (int.TryParse(rawValue, out var catFlagInt))
            {
                var category = (AgeCategoryFilter)catFlagInt;
                var toggleResult = await editingService.ToggleAgeCategoryAsync(user.TelegramId, category, cancellationToken);
                if (toggleResult.IsSuccess && messageId.HasValue)
                {
                    await botClient.EditMessageReplyMarkup(
                        chatId: chatId,
                        messageId: messageId.Value,
                        replyMarkup: ProfileKeyboards.GetSearchPreferencesKeyboard(toggleResult.Value, lang),
                        cancellationToken: cancellationToken
                    );
                }
            }
            return true;
        }

        if (data == "edit_age_cat_save")
        {
            await editingService.SaveAgeCategoriesAsync(user.TelegramId, cancellationToken);
            await SendUpdatedProfileAsync(chatId, user.TelegramId, messageId, cancellationToken);
            return true;
        }

        if (data == "edit:custom_age_range")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_SearchMinAge, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_SearchMinAge, messageId, null, cancellationToken);
            return true;
        }

        if (data == "edit:search_distance")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_SearchDistance, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_SearchDistance, messageId, null, cancellationToken);
            return true;
        }

        if (data.StartsWith("edit_distance:"))
        {
            var rawValue = data["edit_distance:".Length..];
            if (int.TryParse(rawValue, out var distInt) && Enum.IsDefined(typeof(SearchDistancePreference), distInt))
            {
                var distance = (SearchDistancePreference)distInt;
                await editingService.UpdateSearchDistanceAsync(user.TelegramId, distance, cancellationToken);
                await SendUpdatedProfileAsync(chatId, user.TelegramId, messageId, cancellationToken);
            }
            return true;
        }

        if (data == "edit:target")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_DatingTarget, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_DatingTarget, messageId, null, cancellationToken);
            return true;
        }

        if (data.StartsWith("edit_target:"))
        {
            var rawValue = data["edit_target:".Length..];
            if (int.TryParse(rawValue, out var targetInt) && Enum.IsDefined(typeof(DatingTarget), targetInt))
            {
                var targetResult = await editingService.UpdateDatingTargetAsync(user.TelegramId, (DatingTarget)targetInt, cancellationToken);
                if (targetResult.IsFailure)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_DatingTarget, messageId, targetResult.ErrorMessage, cancellationToken);
                    return true;
                }

                await SendUpdatedProfileAsync(chatId, user.TelegramId, messageId, cancellationToken);
            }
            return true;
        }

        if (data == "edit:interests")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_Interests, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Interests, messageId, null, cancellationToken);
            return true;
        }

        if (data.StartsWith("edit_interest_toggle:"))
        {
            var rawValue = data["edit_interest_toggle:".Length..];
            if (int.TryParse(rawValue, out var codeInt) && Enum.IsDefined(typeof(InterestType), codeInt))
            {
                var toggleResult = await editingService.ToggleEditInterestAsync(user.TelegramId, (InterestType)codeInt, cancellationToken);
                if (toggleResult.IsSuccess && messageId.HasValue)
                {
                    await botClient.EditMessageReplyMarkup(
                        chatId: chatId,
                        messageId: messageId.Value,
                        replyMarkup: ProfileKeyboards.GetEditInterestsKeyboard(toggleResult.Value!, lang),
                        cancellationToken: cancellationToken
                    );
                }
            }
            return true;
        }

        if (data == "edit_interest_save")
        {
            await editingService.SaveEditInterestsAsync(user.TelegramId, cancellationToken);
            await SendUpdatedProfileAsync(chatId, user.TelegramId, messageId, cancellationToken);
            return true;
        }

        if (data == "edit:ai_bio")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_AiBio, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_AiBio, messageId, null, cancellationToken);
            return true;
        }

        if (data == "edit:greeting")
        {
            await editingService.SetEditingStateAsync(user.TelegramId, UserState.Editing_Greeting, cancellationToken);
            await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Greeting, messageId, null, cancellationToken);
            return true;
        }

        return false;
    }

    private async Task SendUpdatedProfileAsync(long chatId, long telegramId, int? prevMsgId, CancellationToken cancellationToken)
    {
        var profile = await registrationService.GetProfileDtoAsync(telegramId, cancellationToken);
        if (profile is not null)
        {
            await profilePromptService.SendProfileCardAsync(chatId, profile, prevMsgId, cancellationToken);
        }
    }
}
