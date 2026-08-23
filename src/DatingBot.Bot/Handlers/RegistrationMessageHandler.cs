using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Bot.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DbUser = DatingBot.Domain.Entities.User;

namespace DatingBot.Bot.Handlers;

public class RegistrationMessageHandler(
    ITelegramBotClient botClient,
    IRegistrationService registrationService,
    ICityRepository cityRepository,
    RegistrationPromptService promptService,
    ILocalizationService loc)
{
    public async Task HandleMessageAsync(DbUser user, Message message, CancellationToken cancellationToken = default)
    {
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim();
        var prevBotMsgId = user.LastBotMessageId;
        var lang = user.Language;

        // Удаляем ответ пользователя для поддержания чистоты чата
        await promptService.DeleteMessageSafeAsync(chatId, message.MessageId, cancellationToken);

        switch (user.State)
        {
            case UserState.None:
            case UserState.Registration_SelectingLanguage:
            case UserState.Registration_SelectingGender:
            case UserState.Registration_SelectingTargetGender:
            case UserState.Registration_SelectingInterests:
            case UserState.Registration_SelectingTarget:
                // В этих состояниях ожидается нажатие кнопок. Напомним пользователю:
                await promptService.SendPromptForStateAsync(chatId, user.State, prevBotMsgId, null, cancellationToken);
                break;

            case UserState.Registration_WaitingForName:
                if (string.IsNullOrWhiteSpace(text))
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForName, prevBotMsgId, loc.Get(lang, "Error_NameEmpty"), cancellationToken);
                    return;
                }

                var nameResult = await registrationService.SetNameAsync(user.TelegramId, text, cancellationToken);
                if (nameResult.IsFailure)
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForName, prevBotMsgId, nameResult.ErrorMessage, cancellationToken);
                    return;
                }

                await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForAge, prevBotMsgId, null, cancellationToken);
                break;

            case UserState.Registration_WaitingForAge:
                if (!int.TryParse(text, out var age))
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForAge, prevBotMsgId, loc.Get(lang, "Error_AgeNumber"), cancellationToken);
                    return;
                }

                var ageResult = await registrationService.SetAgeAsync(user.TelegramId, age, cancellationToken);
                if (ageResult.IsFailure)
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForAge, prevBotMsgId, ageResult.ErrorMessage, cancellationToken);
                    return;
                }

                await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForCity, prevBotMsgId, null, cancellationToken);
                break;

            case UserState.Registration_WaitingForCity:
                if (string.IsNullOrWhiteSpace(text))
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForCity, prevBotMsgId, loc.Get(lang, "City_TypeManually"), cancellationToken);
                    return;
                }

                var exactCity = await cityRepository.FindExactByNameAsync(text, cancellationToken);
                if (exactCity is not null)
                {
                    await registrationService.SetCityAsync(user.TelegramId, exactCity.Name, cancellationToken);
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForHeight, prevBotMsgId, null, cancellationToken);
                    return;
                }

                // Проверяем подсказки опечаток
                var suggestions = await cityRepository.SearchSuggestionsAsync(text, limit: 1, cancellationToken);
                if (suggestions.Count > 0)
                {
                    var suggested = suggestions[0];
                    if (!string.Equals(suggested.Name, text, StringComparison.OrdinalIgnoreCase))
                    {
                        if (prevBotMsgId.HasValue)
                        {
                            await promptService.DeleteMessageSafeAsync(chatId, prevBotMsgId.Value, cancellationToken);
                        }

                        var msg = await botClient.SendMessage(
                            chatId: chatId,
                            text: loc.Get(lang, "City_DidYouMean", suggested.Name),
                            parseMode: ParseMode.Html,
                            replyMarkup: SearchKeyboards.GetCitySuggestionKeyboard(suggested.Id, suggested.Name, isEditing: false),
                            cancellationToken: cancellationToken
                        );

                        await registrationService.SaveLastBotMessageIdAsync(chatId, msg.MessageId, cancellationToken);
                        return;
                    }
                }

                var cityResult = await registrationService.SetCityAsync(user.TelegramId, text, cancellationToken);
                if (cityResult.IsFailure)
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForCity, prevBotMsgId, cityResult.ErrorMessage, cancellationToken);
                    return;
                }

                await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForHeight, prevBotMsgId, null, cancellationToken);
                break;

            case UserState.Registration_WaitingForHeight:
                if (!int.TryParse(text, out var height))
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForHeight, prevBotMsgId, loc.Get(lang, "Error_HeightNumber"), cancellationToken);
                    return;
                }

                var heightResult = await registrationService.SetHeightAsync(user.TelegramId, height, cancellationToken);
                if (heightResult.IsFailure)
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForHeight, prevBotMsgId, heightResult.ErrorMessage, cancellationToken);
                    return;
                }

                await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForPhoto, prevBotMsgId, null, cancellationToken);
                break;

            case UserState.Registration_WaitingForPhoto:
                if (message.Photo is null || message.Photo.Length == 0)
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForPhoto, prevBotMsgId, loc.Get(lang, "Error_PhotoRequired"), cancellationToken);
                    return;
                }

                var largestPhoto = message.Photo[^1];
                var photoResult = await registrationService.SetPhotoAsync(user.TelegramId, largestPhoto.FileId, cancellationToken);
                if (photoResult.IsFailure)
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForPhoto, prevBotMsgId, photoResult.ErrorMessage, cancellationToken);
                    return;
                }

                await promptService.SendPromptForStateAsync(chatId, UserState.Registration_SelectingInterests, prevBotMsgId, null, cancellationToken);
                break;

            case UserState.Registration_WaitingForAiBio:
                if (string.IsNullOrWhiteSpace(text))
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForAiBio, prevBotMsgId, loc.Get(lang, "Error_AiBioEmpty"), cancellationToken);
                    return;
                }

                var completeResult = await registrationService.SetAiDescriptionAndCompleteAsync(user.TelegramId, text, cancellationToken);
                if (completeResult.IsFailure)
                {
                    await promptService.SendPromptForStateAsync(chatId, UserState.Registration_WaitingForAiBio, prevBotMsgId, completeResult.ErrorMessage, cancellationToken);
                    return;
                }

                // Показываем финальную готовую анкету!
                await promptService.SendCompletedProfileCardAsync(chatId, completeResult.Value!, prevBotMsgId, cancellationToken);
                break;

            case UserState.Active:
                await botClient.SendMessage(chatId, loc.Get(lang, "Active_ProfileActive"), cancellationToken: cancellationToken);
                break;
        }
    }
}
