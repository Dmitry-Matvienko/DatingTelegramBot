using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Bot.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using DbUser = DatingBot.Domain.Entities.User;

namespace DatingBot.Bot.Handlers;

public class RegistrationCallbackHandler(
    ITelegramBotClient botClient,
    IRegistrationService registrationService,
    ICityRepository cityRepository,
    RegistrationPromptService promptService,
    ILocalizationService loc)
{
    public async Task HandleCallbackQueryAsync(DbUser user, CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var data = callbackQuery.Data ?? string.Empty;
        var chatId = callbackQuery.Message?.Chat.Id ?? user.TelegramId;
        var messageId = callbackQuery.Message?.MessageId;
        var lang = user.Language;

        // Всегда подтверждаем callback-запрос
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

        if (data.StartsWith("reg_lang_"))
        {
            var rawLang = data["reg_lang_".Length..];
            if (int.TryParse(rawLang, out var langInt) && Enum.IsDefined(typeof(AppLanguage), langInt))
            {
                var language = (AppLanguage)langInt;
                await registrationService.SetLanguageAsync(user.TelegramId, language, cancellationToken);
                await promptService.SendPromptForStateAsync(chatId, UserState.Registration_SelectingGender, messageId, null, cancellationToken);
                return;
            }
        }
        else if (data.StartsWith("reg_city_confirm:"))
        {
            var rawId = data["reg_city_confirm:".Length..];
            if (int.TryParse(rawId, out var cityId))
            {
                var city = await cityRepository.GetByIdAsync(cityId, cancellationToken);
                if (city is not null)
                {
                    await registrationService.SetCityAsync(user.TelegramId, city.Name, cancellationToken);
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForHeight, messageId, null, cancellationToken);
                    return;
                }
            }

            await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForCity, messageId, loc.Get(lang, "City_TypeManually"), cancellationToken);
        }
        else if (data == "reg_city_retry")
        {
            await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForCity, messageId, loc.Get(lang, "City_TypeManually"), cancellationToken);
        }
        else if (data.StartsWith("gender_set:"))
        {
            if (user.State != UserState.Registration_SelectingGender && user.State != UserState.None)
            {
                await promptService.DeleteMessageSafeAsync(chatId, messageId, cancellationToken);
                return;
            }

            var rawValue = data["gender_set:".Length..];
            if (int.TryParse(rawValue, out var genderInt) && Enum.IsDefined(typeof(Gender), genderInt))
            {
                var gender = (Gender)genderInt;
                await registrationService.SetGenderAsync(user.TelegramId, gender, cancellationToken);
                await promptService.SendPromptForStateAsync(chatId, UserState.Registration_SelectingTargetGender, messageId, null, cancellationToken);
            }
        }
        else if (data.StartsWith("target_gender_set:"))
        {
            if (user.State != UserState.Registration_SelectingTargetGender)
            {
                await promptService.DeleteMessageSafeAsync(chatId, messageId, cancellationToken);
                return;
            }

            var rawValue = data["target_gender_set:".Length..];
            if (int.TryParse(rawValue, out var tgInt) && Enum.IsDefined(typeof(TargetGender), tgInt))
            {
                var targetGender = (TargetGender)tgInt;
                await registrationService.SetTargetGenderAsync(user.TelegramId, targetGender, cancellationToken);
                await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForName, messageId, null, cancellationToken);
            }
        }
        else if (data == "height_skip")
        {
            if (user.State != UserState.Registration_WaitingForHeight)
            {
                await promptService.DeleteMessageSafeAsync(chatId, messageId, cancellationToken);
                return;
            }

            await registrationService.SkipHeightAsync(user.TelegramId, cancellationToken);
            await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForPhoto, messageId, null, cancellationToken);
        }
        else if (data.StartsWith("interest_toggle:"))
        {
            if (user.State != UserState.Registration_SelectingInterests)
            {
                await promptService.DeleteMessageSafeAsync(chatId, messageId, cancellationToken);
                return;
            }

            var rawValue = data["interest_toggle:".Length..];
            if (int.TryParse(rawValue, out var codeInt) && Enum.IsDefined(typeof(InterestType), codeInt))
            {
                var code = (InterestType)codeInt;
                var toggleResult = await registrationService.ToggleInterestAsync(user.TelegramId, code, cancellationToken);
                if (toggleResult.IsSuccess && messageId.HasValue)
                {
                    // Обновляем разметку клавиатуры на месте с галочками
                    await botClient.EditMessageReplyMarkup(
                        chatId: chatId,
                        messageId: messageId.Value,
                        replyMarkup: RegistrationKeyboards.GetInterestsKeyboard(toggleResult.Value!, user.Language),
                        cancellationToken: cancellationToken
                    );
                }
            }
        }
        else if (data == "interest_done")
        {
            if (user.State != UserState.Registration_SelectingInterests)
            {
                await promptService.DeleteMessageSafeAsync(chatId, messageId, cancellationToken);
                return;
            }

            var completeResult = await registrationService.CompleteInterestsAsync(user.TelegramId, cancellationToken);
            if (completeResult.IsSuccess)
            {
                await promptService.SendPromptForStateAsync(chatId, UserState.Registration_SelectingTarget, messageId, null, cancellationToken);
            }
        }
        else if (data.StartsWith("target_set:"))
        {
            if (user.State != UserState.Registration_SelectingTarget)
            {
                await promptService.DeleteMessageSafeAsync(chatId, messageId, cancellationToken);
                return;
            }

            var rawValue = data["target_set:".Length..];
            if (int.TryParse(rawValue, out var targetInt) && Enum.IsDefined(typeof(DatingTarget), targetInt))
            {
                var target = (DatingTarget)targetInt;
                var targetResult = await registrationService.SetDatingTargetAsync(user.TelegramId, target, cancellationToken);
                if (targetResult.IsFailure)
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_SelectingTarget, messageId, targetResult.ErrorMessage, cancellationToken);
                    return;
                }

                await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForAiBio, messageId, null, cancellationToken);
            }
        }
    }
}
