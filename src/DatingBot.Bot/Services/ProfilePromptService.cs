using System.Text;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Domain.Enums;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DatingBot.Bot.Services;

public class ProfilePromptService(
    ITelegramBotClient botClient,
    IRegistrationService registrationService,
    IUserRepository userRepository,
    ILocalizationService loc,
    RegistrationPromptService registrationPromptService,
    ILogger<ProfilePromptService> logger)
{
    public async Task SendProfileCardAsync(long chatId, UserProfileDto profile, int? previousMessageIdToDelete = null, CancellationToken cancellationToken = default)
    {
        if (previousMessageIdToDelete.HasValue)
        {
            await registrationPromptService.DeleteMessageSafeAsync(chatId, previousMessageIdToDelete.Value, cancellationToken);
        }

        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var targetStr = loc.GetDatingTargetText(lang, profile.DatingTarget);
        var genderStr = loc.GetGenderText(lang, profile.Gender);
        var lookingForStr = loc.GetTargetGenderText(lang, profile.TargetGender);

        string searchAgeStr;
        if (profile.SearchMinAge.HasValue && profile.SearchMaxAge.HasValue)
        {
            searchAgeStr = $"{profile.SearchMinAge} – {profile.SearchMaxAge}";
        }
        else if (profile.AgeFilters != AgeCategoryFilter.None)
        {
            var categories = new List<string>();
            if (profile.AgeFilters.HasFlag(AgeCategoryFilter.Under18)) categories.Add("< 18");
            if (profile.AgeFilters.HasFlag(AgeCategoryFilter.Age18To25)) categories.Add("18–25");
            if (profile.AgeFilters.HasFlag(AgeCategoryFilter.Age25To30)) categories.Add("25–30");
            if (profile.AgeFilters.HasFlag(AgeCategoryFilter.Age30To40)) categories.Add("30–40");
            if (profile.AgeFilters.HasFlag(AgeCategoryFilter.Age40Plus)) categories.Add("40+");
            searchAgeStr = string.Join(", ", categories);
        }
        else
        {
            searchAgeStr = "-";
        }

        string distanceStr = profile.SearchDistance switch
        {
            SearchDistancePreference.UpTo100Km => loc.Get(lang, "Distance_UpTo100Km"),
            SearchDistancePreference.UpTo500Km => loc.Get(lang, "Distance_UpTo500Km"),
            SearchDistancePreference.SameCountry => loc.Get(lang, "Distance_SameCountry"),
            SearchDistancePreference.Anywhere => loc.Get(lang, "Distance_Anywhere"),
            _ => loc.Get(lang, "Distance_UpTo500Km")
        };

        var sb = new StringBuilder();
        sb.AppendLine($"👤 <b>{profile.Name}</b>, {profile.Age}\n");
        sb.AppendLine($"📍 <b>{loc.Get(lang, "Label_City")}:</b> {profile.City}");
        sb.AppendLine($"📏 <b>{loc.Get(lang, "Label_Height")}:</b> {(profile.Height.HasValue ? $"{profile.Height} cm" : "-")}");
        sb.AppendLine($"🚻 <b>{loc.Get(lang, "Label_Gender")}:</b> {genderStr}");
        sb.AppendLine($"🔍 <b>{loc.Get(lang, "Label_LookingFor")}:</b> {lookingForStr}");
        sb.AppendLine($"🔎 <b>{loc.Get(lang, "Label_AgeFilters")}:</b> {searchAgeStr}");
        sb.AppendLine($"📍 <b>{loc.Get(lang, "Label_SearchDistance")}:</b> {distanceStr}");
        sb.AppendLine($"🎯 <b>{loc.Get(lang, "Label_Target")}:</b> {targetStr}");

        var ratingStr = profile.RatingCount > 0
            ? $"⭐ {profile.AverageRating:F1} / 10 ({profile.RatingCount})"
            : $"⭐ {loc.Get(lang, "Label_NoRatingsYet")}";
        sb.AppendLine($"📊 <b>{loc.Get(lang, "Label_MyRating")}:</b> {ratingStr}");

        if (!string.IsNullOrWhiteSpace(profile.AiDescription))
        {
            sb.AppendLine($"\n{loc.Get(lang, "Label_AiBioSecret")}\n<i>\"{profile.AiDescription}\"</i>");
        }

        if (!string.IsNullOrWhiteSpace(profile.Greeting))
        {
            sb.AppendLine($"\n💬 <b>{loc.Get(lang, "Label_Greeting")}:</b>\n<i>\"{profile.Greeting}\"</i>");
        }

        if (profile.SelectedInterests.Count > 0)
        {
            var interestTags = string.Join(", ", profile.SelectedInterests.Select(i => $"{i.Icon} {loc.GetInterestTitle(lang, i.Code.ToString().ToLowerInvariant(), i.Title)}"));
            sb.AppendLine($"\n🏷 <b>{loc.Get(lang, "Label_Interests")}:</b> {interestTags}");
        }

        sb.AppendLine($"\n{loc.Get(lang, "Prompt_ClickButtonToEdit")}");

        Message sentMessage = null!;
        var photoSent = false;
        var cardText = sb.ToString();
        if (!string.IsNullOrEmpty(profile.PhotoFileId) && cardText.Length <= 1024)
        {
            try
            {
                sentMessage = await botClient.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromFileId(profile.PhotoFileId),
                    caption: cardText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetProfileEditKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                photoSent = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Не удалось отправить фото профиля {PhotoFileId} пользователю {ChatId}: {ErrorMessage}", profile.PhotoFileId, chatId, ex.Message);
                sentMessage = null!;
            }
        }

        if (!photoSent)
        {
            sentMessage = await botClient.SendMessage(
                chatId: chatId,
                text: cardText,
                parseMode: ParseMode.Html,
                replyMarkup: ProfileKeyboards.GetProfileEditKeyboard(lang),
                cancellationToken: cancellationToken
            );
        }

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
    }

    public async Task SendEditPromptAsync(long chatId, UserState state, int? previousMessageIdToDelete = null, string? customErrorMessage = null, CancellationToken cancellationToken = default)
    {
        if (previousMessageIdToDelete.HasValue)
        {
            await registrationPromptService.DeleteMessageSafeAsync(chatId, previousMessageIdToDelete.Value, cancellationToken);
        }

        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        Message sentMessage;

        switch (state)
        {
            case UserState.Editing_Language:
                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "LanguagePrompt"),
                    parseMode: ParseMode.Html,
                    replyMarkup: LanguageKeyboards.GetLanguageSelectionKeyboard("edit_lang"),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_Name:
                var nameText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "NamePrompt")}"
                    : loc.Get(lang, "NamePrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: nameText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetCancelKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_Age:
                var ageText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "AgePrompt")}"
                    : loc.Get(lang, "AgePrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: ageText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetCancelKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_City:
                var cityText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "CityPrompt")}"
                    : loc.Get(lang, "CityPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: cityText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetCancelKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_Height:
                var heightText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "HeightPrompt")}"
                    : loc.Get(lang, "HeightPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: heightText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetEditHeightKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_Photo:
                var photoText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "PhotoPrompt")}"
                    : loc.Get(lang, "PhotoPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: photoText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetCancelKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_Gender:
                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "GenderPrompt"),
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetEditGenderKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_TargetGender:
                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "TargetGenderPrompt"),
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetEditTargetGenderKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_DatingTarget:
                var targetText = customErrorMessage != null
                    ? $"⚠️ <b>{customErrorMessage}</b>\n\n{loc.Get(lang, "TargetPrompt")}"
                    : loc.Get(lang, "TargetPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: targetText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetEditDatingTargetKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_Interests:
                var interests = await registrationService.GetUserInterestsAsync(chatId, cancellationToken);
                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "InterestsPrompt"),
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetEditInterestsKeyboard(interests, lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_SearchAgeCategories:
                var currentProfile = await registrationService.GetProfileDtoAsync(chatId, cancellationToken);
                var ageFilters = currentProfile?.AgeFilters ?? AgeCategoryFilter.None;

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "Prompt_SearchAgePreferences"),
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetSearchPreferencesKeyboard(ageFilters, lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_SearchMinAge:
                var minAgePrompt = loc.Get(lang, "Prompt_MinAge");
                var minAgeText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{minAgePrompt}"
                    : minAgePrompt;

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: minAgeText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetCancelKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_SearchMaxAge:
                var profileForMax = await registrationService.GetProfileDtoAsync(chatId, cancellationToken);
                var currentMin = profileForMax?.SearchMinAge ?? 10;

                var maxAgePrompt = loc.Get(lang, "Prompt_MaxAge", currentMin);
                var maxAgeText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{maxAgePrompt}"
                    : maxAgePrompt;

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: maxAgeText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetCancelKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_AiBio:
                var bioText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "AiBioPrompt")}"
                    : loc.Get(lang, "AiBioPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: bioText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetCancelKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_Greeting:
                var greetingText = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "GreetingPrompt")}"
                    : loc.Get(lang, "GreetingPrompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: greetingText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetCancelKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                break;

            case UserState.Editing_SearchDistance:
                var currentProf = await registrationService.GetProfileDtoAsync(chatId, cancellationToken);
                var currentDist = currentProf?.SearchDistance ?? SearchDistancePreference.UpTo500Km;

                var distPrompt = customErrorMessage != null
                    ? $"❌ {customErrorMessage}\n\n{loc.Get(lang, "SearchDistance_Prompt")}"
                    : loc.Get(lang, "SearchDistance_Prompt");

                sentMessage = await botClient.SendMessage(
                    chatId: chatId,
                    text: distPrompt,
                    parseMode: ParseMode.Html,
                    replyMarkup: ProfileKeyboards.GetEditSearchDistanceKeyboard(currentDist, lang),
                    cancellationToken: cancellationToken
                );
                break;

            default:
                return;
        }

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
    }

    public async Task SendMainMenuGreetingAsync(long chatId, int? previousMessageIdToDelete = null, CancellationToken cancellationToken = default)
    {
        if (previousMessageIdToDelete.HasValue)
        {
            await registrationPromptService.DeleteMessageSafeAsync(chatId, previousMessageIdToDelete.Value, cancellationToken);
        }

        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: loc.Get(lang, "MainMenuGreeting"),
            parseMode: ParseMode.Html,
            replyMarkup: MainMenuKeyboards.GetMainMenuReplyKeyboard(lang),
            cancellationToken: cancellationToken
        );

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
    }
}
