using System.Text;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DatingBot.Bot.Services;

public class RegistrationPromptService(
    ITelegramBotClient botClient,
    IRegistrationService registrationService,
    IUserRepository userRepository,
    ILocalizationService loc)
{
    public async Task DeleteMessageSafeAsync(long chatId, int? messageId, CancellationToken cancellationToken = default)
    {
        if (messageId.HasValue)
        {
            try
            {
                await botClient.DeleteMessage(chatId, messageId.Value, cancellationToken);
            }
            catch
            {
                // Игнорируем ошибку, если сообщение уже удалено или недоступно
            }
        }
    }

    public async Task SendPromptForStateAsync(long chatId, UserState state, int? previousMessageIdToDelete = null, string? customErrorMessage = null, CancellationToken cancellationToken = default)
    {
        if (previousMessageIdToDelete.HasValue)
        {
            await DeleteMessageSafeAsync(chatId, previousMessageIdToDelete.Value, cancellationToken);
        }

        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        Message sentMessage;

        switch (state)
        {
            case UserState.None:
            case UserState.Registration_SelectingLanguage:
                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "LanguagePrompt"),
                    parseMode: ParseMode.Html,
                    replyMarkup: LanguageKeyboards.GetLanguageSelectionKeyboard("reg_lang"),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Registration_SelectingGender:
                var welcome = loc.Get(lang, "WelcomeTitle");
                var genderText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{welcome}\n\n{loc.Get(lang, "GenderPrompt")}"
                    : $"{welcome}\n\n{loc.Get(lang, "GenderPrompt")}";

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: genderText,
                    parseMode: ParseMode.Html,
                    replyMarkup: RegistrationKeyboards.GetGenderKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Registration_SelectingTargetGender:
                var targetGenderText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "TargetGenderPrompt")}"
                    : loc.Get(lang, "TargetGenderPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: targetGenderText,
                    parseMode: ParseMode.Html,
                    replyMarkup: RegistrationKeyboards.GetTargetGenderKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Registration_WaitingForName:
                var nameText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "NamePrompt")}"
                    : loc.Get(lang, "NamePrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: nameText,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Registration_WaitingForAge:
                var ageText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "AgePrompt")}"
                    : loc.Get(lang, "AgePrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: ageText,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Registration_WaitingForCity:
                var cityText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "CityPrompt")}"
                    : loc.Get(lang, "CityPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: cityText,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Registration_WaitingForHeight:
                var heightText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "HeightPrompt")}"
                    : loc.Get(lang, "HeightPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: heightText,
                    parseMode: ParseMode.Html,
                    replyMarkup: RegistrationKeyboards.GetHeightKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Registration_WaitingForPhoto:
                var photoText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "PhotoPrompt")}"
                    : loc.Get(lang, "PhotoPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: photoText,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Registration_SelectingInterests:
                var interests = await registrationService.GetUserInterestsAsync(chatId, cancellationToken);
                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "InterestsPrompt"),
                    parseMode: ParseMode.Html,
                    replyMarkup: RegistrationKeyboards.GetInterestsKeyboard(interests, lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Registration_SelectingTarget:
                var targetPromptText = customErrorMessage != null
                    ? $"⚠️ <b>{customErrorMessage}</b>\n\n{loc.Get(lang, "TargetPrompt")}"
                    : loc.Get(lang, "TargetPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: targetPromptText,
                    parseMode: ParseMode.Html,
                    replyMarkup: RegistrationKeyboards.GetDatingTargetKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Registration_WaitingForAiBio:
                var aiBioText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "AiBioPrompt")}"
                    : loc.Get(lang, "AiBioPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: aiBioText,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );
                break;

            default:
                return;
        }

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
    }

    public async Task SendCompletedProfileCardAsync(long chatId, UserProfileDto profile, int? previousMessageIdToDelete = null, CancellationToken cancellationToken = default)
    {
        if (previousMessageIdToDelete.HasValue)
        {
            await DeleteMessageSafeAsync(chatId, previousMessageIdToDelete.Value, cancellationToken);
        }

        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var targetStr = loc.GetDatingTargetText(lang, profile.DatingTarget);
        var genderStr = loc.GetGenderText(lang, profile.Gender);
        var lookingForStr = loc.GetTargetGenderText(lang, profile.TargetGender);

        var sb = new StringBuilder();
        sb.AppendLine($"🎉 <b>{profile.Name}</b>, {profile.Age}\n");
        sb.AppendLine($"📍 {loc.Get(lang, "Label_City")}: <b>{profile.City}</b>");
        if (profile.Height.HasValue)
        {
            sb.AppendLine($"📏 {loc.Get(lang, "Label_Height")}: <b>{profile.Height} cm</b>");
        }
        sb.AppendLine($"🚻 {loc.Get(lang, "Label_Gender")}: <b>{genderStr}</b> ({loc.Get(lang, "Label_LookingFor")}: <b>{lookingForStr}</b>)");
        sb.AppendLine($"🎯 {loc.Get(lang, "Label_Target")}: <b>{targetStr}</b>");

        if (profile.SelectedInterests.Count > 0)
        {
            var interestTags = string.Join(", ", profile.SelectedInterests.Select(i => $"{i.Icon} {loc.GetInterestTitle(lang, i.Code.ToString().ToLowerInvariant(), i.Title)}"));
            sb.AppendLine($"\n🏷 <b>{loc.Get(lang, "Label_Interests")}:</b> {interestTags}");
        }

        Message sentCard = null!;
        var photoSent = false;
        var cardText = sb.ToString();
        if (!string.IsNullOrEmpty(profile.PhotoFileId) && cardText.Length <= 1024)
        {
            try
            {
                sentCard = await botClient.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromFileId(profile.PhotoFileId),
                    caption: cardText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetProfileEditKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                photoSent = true;
            }
            catch
            {
                sentCard = null!;
            }
        }

        if (!photoSent)
        {
            sentCard = await botClient.SendMessage(
                chatId: chatId,
                text: cardText,
                parseMode: ParseMode.Html,
                replyMarkup: ProfileKeyboards.GetProfileEditKeyboard(lang),
                cancellationToken: cancellationToken
            );
        }

        await botClient.SendMessage(
            chatId: chatId,
            text: "👇",
            replyMarkup: MainMenuKeyboards.GetMainMenuReplyKeyboard(lang),
            cancellationToken: cancellationToken
        );

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentCard.MessageId, cancellationToken);
    }
}
