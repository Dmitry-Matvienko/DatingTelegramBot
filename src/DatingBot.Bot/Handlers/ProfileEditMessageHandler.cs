using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Bot.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DbUser = DatingBot.Domain.Entities.User;

namespace DatingBot.Bot.Handlers;

public class ProfileEditMessageHandler(
    ITelegramBotClient botClient,
    IProfileEditingService editingService,
    IRegistrationService registrationService,
    ICityRepository cityRepository,
    ProfilePromptService profilePromptService,
    RegistrationPromptService registrationPromptService,
    ILocalizationService loc)
{
    public async Task<bool> HandleEditMessageAsync(DbUser user, Message message, CancellationToken cancellationToken = default)
    {
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim();
        var prevMsgId = user.LastBotMessageId;
        var lang = user.Language;

        // Удаляем ввод пользователя для поддержания чистоты чата
        await registrationPromptService.DeleteMessageSafeAsync(chatId, message.MessageId, cancellationToken);

        switch (user.State)
        {
            case UserState.Editing_Language:
            case UserState.Editing_SearchDistance:
                await profilePromptService.SendEditPromptAsync(chatId, user.State, prevMsgId, null, cancellationToken);
                return true;

            case UserState.Editing_Name:
                if (string.IsNullOrWhiteSpace(text))
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Name, prevMsgId, loc.Get(lang, "Error_NameEmpty"), cancellationToken);
                    return true;
                }

                var nameResult = await editingService.UpdateNameAsync(user.TelegramId, text, cancellationToken);
                if (nameResult.IsFailure)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Name, prevMsgId, nameResult.ErrorMessage, cancellationToken);
                    return true;
                }

                await SendUpdatedProfileAsync(chatId, user.TelegramId, prevMsgId, cancellationToken);
                return true;

            case UserState.Editing_Age:
                if (!int.TryParse(text, out var age))
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Age, prevMsgId, loc.Get(lang, "Error_AgeNumber"), cancellationToken);
                    return true;
                }

                var ageResult = await editingService.UpdateAgeAsync(user.TelegramId, age, cancellationToken);
                if (ageResult.IsFailure)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Age, prevMsgId, ageResult.ErrorMessage, cancellationToken);
                    return true;
                }

                await SendUpdatedProfileAsync(chatId, user.TelegramId, prevMsgId, cancellationToken);
                return true;

            case UserState.Editing_City:
                if (string.IsNullOrWhiteSpace(text))
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_City, prevMsgId, loc.Get(lang, "City_TypeManually"), cancellationToken);
                    return true;
                }

                var exactCity = await cityRepository.FindExactByNameAsync(text, cancellationToken);
                if (exactCity is not null)
                {
                    await editingService.UpdateCityAsync(user.TelegramId, exactCity.Name, cancellationToken);
                    await SendUpdatedProfileAsync(chatId, user.TelegramId, prevMsgId, cancellationToken);
                    return true;
                }

                var suggestions = await cityRepository.SearchSuggestionsAsync(text, limit: 1, cancellationToken);
                if (suggestions.Count > 0)
                {
                    var suggested = suggestions[0];
                    if (!string.Equals(suggested.Name, text, StringComparison.OrdinalIgnoreCase))
                    {
                        if (prevMsgId.HasValue)
                        {
                            await registrationPromptService.DeleteMessageSafeAsync(chatId, prevMsgId.Value, cancellationToken);
                        }

                        var msg = await botClient.SendMessage(
                            chatId: chatId,
                            text: loc.Get(lang, "City_DidYouMean", suggested.Name),
                            parseMode: ParseMode.Html,
                            replyMarkup: SearchKeyboards.GetCitySuggestionKeyboard(suggested.Id, suggested.Name, isEditing: true),
                            cancellationToken: cancellationToken
                        );

                        await registrationService.SaveLastBotMessageIdAsync(chatId, msg.MessageId, cancellationToken);
                        return true;
                    }
                }

                var cityResult = await editingService.UpdateCityAsync(user.TelegramId, text, cancellationToken);
                if (cityResult.IsFailure)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_City, prevMsgId, cityResult.ErrorMessage, cancellationToken);
                    return true;
                }

                await SendUpdatedProfileAsync(chatId, user.TelegramId, prevMsgId, cancellationToken);
                return true;

            case UserState.Editing_Height:
                if (!int.TryParse(text, out var height))
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Height, prevMsgId, loc.Get(lang, "Error_HeightNumber"), cancellationToken);
                    return true;
                }

                var heightResult = await editingService.UpdateHeightAsync(user.TelegramId, height, cancellationToken);
                if (heightResult.IsFailure)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Height, prevMsgId, heightResult.ErrorMessage, cancellationToken);
                    return true;
                }

                await SendUpdatedProfileAsync(chatId, user.TelegramId, prevMsgId, cancellationToken);
                return true;

            case UserState.Editing_Photo:
                if (message.Photo is null || message.Photo.Length == 0)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Photo, prevMsgId, loc.Get(lang, "Error_PhotoRequired"), cancellationToken);
                    return true;
                }

                var largestPhoto = message.Photo[^1];
                var photoResult = await editingService.UpdatePhotoAsync(user.TelegramId, largestPhoto.FileId, cancellationToken);
                if (photoResult.IsFailure)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Photo, prevMsgId, photoResult.ErrorMessage, cancellationToken);
                    return true;
                }

                await SendUpdatedProfileAsync(chatId, user.TelegramId, prevMsgId, cancellationToken);
                return true;

            case UserState.Editing_AiBio:
                if (string.IsNullOrWhiteSpace(text))
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_AiBio, prevMsgId, loc.Get(lang, "Error_AiBioEmpty"), cancellationToken);
                    return true;
                }

                var bioResult = await editingService.UpdateAiBioAsync(user.TelegramId, text, cancellationToken);
                if (bioResult.IsFailure)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_AiBio, prevMsgId, bioResult.ErrorMessage, cancellationToken);
                    return true;
                }

                await SendUpdatedProfileAsync(chatId, user.TelegramId, prevMsgId, cancellationToken);
                return true;

            case UserState.Editing_Greeting:
                if (string.IsNullOrWhiteSpace(text))
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Greeting, prevMsgId, loc.Get(lang, "Error_GreetingEmpty"), cancellationToken);
                    return true;
                }

                var greetingResult = await editingService.UpdateGreetingAsync(user.TelegramId, text, cancellationToken);
                if (greetingResult.IsFailure)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_Greeting, prevMsgId, greetingResult.ErrorMessage, cancellationToken);
                    return true;
                }

                await SendUpdatedProfileAsync(chatId, user.TelegramId, prevMsgId, cancellationToken);
                return true;

            case UserState.Editing_SearchMinAge:
                if (!int.TryParse(text, out var minAge) || minAge < 10 || minAge > 100)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_SearchMinAge, prevMsgId, loc.Get(lang, "Error_AgeNumber"), cancellationToken);
                    return true;
                }

                var minAgeResult = await editingService.SetSearchMinAgeAsync(user.TelegramId, minAge, cancellationToken);
                if (minAgeResult.IsFailure)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_SearchMinAge, prevMsgId, minAgeResult.ErrorMessage, cancellationToken);
                    return true;
                }

                await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_SearchMaxAge, prevMsgId, null, cancellationToken);
                return true;

            case UserState.Editing_SearchMaxAge:
                if (!int.TryParse(text, out var maxAge) || maxAge < 10 || maxAge > 100)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_SearchMaxAge, prevMsgId, loc.Get(lang, "Error_AgeNumber"), cancellationToken);
                    return true;
                }

                var maxAgeResult = await editingService.SetSearchMaxAgeAsync(user.TelegramId, maxAge, cancellationToken);
                if (maxAgeResult.IsFailure)
                {
                    await profilePromptService.SendEditPromptAsync(chatId, UserState.Editing_SearchMaxAge, prevMsgId, maxAgeResult.ErrorMessage, cancellationToken);
                    return true;
                }

                await SendUpdatedProfileAsync(chatId, user.TelegramId, prevMsgId, cancellationToken);
                return true;

            default:
                return false;
        }
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
