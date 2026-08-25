using System.Text;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DatingBot.Bot.Services;

public class SearchPromptService(
    ITelegramBotClient botClient,
    IRegistrationService registrationService,
    IUserRepository userRepository,
    ILocalizationService loc,
    IConfiguration configuration,
    ILogger<SearchPromptService> logger)
{
    public async Task SendMatchCandidateCardAsync(long chatId, MatchCandidateDto match, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var candidate = match.Profile;
        var targetStr = loc.GetDatingTargetText(lang, candidate.DatingTarget);
        var genderStr = loc.GetGenderText(lang, candidate.Gender);
        var lookingForStr = loc.GetTargetGenderText(lang, candidate.TargetGender);

        var sb = new StringBuilder();
        sb.AppendLine($"👤 <b>{candidate.Name}</b>, {candidate.Age}");
        sb.AppendLine($"📍 {loc.Get(lang, "Label_City")}: <b>{candidate.City}</b>");
        if (candidate.Height.HasValue)
        {
            sb.AppendLine($"📏 {loc.Get(lang, "Label_Height")}: <b>{candidate.Height} cm</b>");
        }
        sb.AppendLine($"🚻 {loc.Get(lang, "Label_Gender")}: <b>{genderStr}</b> ({loc.Get(lang, "Label_LookingFor")}: <b>{lookingForStr}</b>)");
        sb.AppendLine($"🎯 {loc.Get(lang, "Label_Target")}: <b>{targetStr}</b>");

        if (!string.IsNullOrWhiteSpace(candidate.Greeting))
        {
            sb.AppendLine($"\n💬 <i>\"{candidate.Greeting}\"</i>");
        }

        // Разделение интересов: общие и другие
        if (match.CommonInterests.Count > 0)
        {
            var commonTags = string.Join(", ", match.CommonInterests.Select(i => $"{i.Icon} {loc.GetInterestTitle(lang, i.Code.ToString().ToLowerInvariant(), i.Title)}"));
            sb.AppendLine($"\n✨ <b>{loc.Get(lang, "Label_CommonInterests")}:</b> {commonTags}");

            if (match.OtherInterests.Count > 0)
            {
                var otherTags = string.Join(", ", match.OtherInterests.Select(i => $"{i.Icon} {loc.GetInterestTitle(lang, i.Code.ToString().ToLowerInvariant(), i.Title)}"));
                sb.AppendLine($"🏷 <b>{loc.Get(lang, "Label_OtherInterests")}:</b> {otherTags}");
            }
        }
        else if (candidate.SelectedInterests.Count > 0)
        {
            var interestTags = string.Join(", ", candidate.SelectedInterests.Select(i => $"{i.Icon} {loc.GetInterestTitle(lang, i.Code.ToString().ToLowerInvariant(), i.Title)}"));
            sb.AppendLine($"\n🏷 <b>{loc.Get(lang, "Label_Interests")}:</b> {interestTags}");
        }

        // Подпись с причиной подбора
        if (!string.IsNullOrWhiteSpace(match.MatchReasonBadge))
        {
            sb.AppendLine($"\n{match.MatchReasonBadge}");
        }

        // Динамическая подсказка в конце анкеты
        var tip = loc.GetRandomSearchTip(lang);
        sb.AppendLine($"\n——\n{tip}");

        Message sentMessage = null!;
        var photoSent = false;
        var cardText = sb.ToString();
        if (!string.IsNullOrEmpty(candidate.PhotoFileId) && cardText.Length <= 1024)
        {
            try
            {
                sentMessage = await botClient.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromFileId(candidate.PhotoFileId),
                    caption: cardText,
                    parseMode: ParseMode.Html,
                    replyMarkup: SearchKeyboards.GetRatingReplyKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                photoSent = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Не удалось отправить фото кандидата {PhotoFileId} пользователю {ChatId}: {ErrorMessage}", candidate.PhotoFileId, chatId, ex.Message);
                sentMessage = null!;
            }
        }

        if (!photoSent)
        {
            sentMessage = await botClient.SendMessage(
                chatId: chatId,
                text: cardText,
                parseMode: ParseMode.Html,
                replyMarkup: SearchKeyboards.GetRatingReplyKeyboard(lang),
                cancellationToken: cancellationToken
            );
        }

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
    }

    public async Task SendCandidateCardAsync(long chatId, UserProfileDto candidate, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var targetStr = loc.GetDatingTargetText(lang, candidate.DatingTarget);
        var genderStr = loc.GetGenderText(lang, candidate.Gender);
        var lookingForStr = loc.GetTargetGenderText(lang, candidate.TargetGender);

        var sb = new StringBuilder();
        sb.AppendLine($"👤 <b>{candidate.Name}</b>, {candidate.Age}");
        sb.AppendLine($"📍 {loc.Get(lang, "Label_City")}: <b>{candidate.City}</b>");
        if (candidate.Height.HasValue)
        {
            sb.AppendLine($"📏 {loc.Get(lang, "Label_Height")}: <b>{candidate.Height} cm</b>");
        }
        sb.AppendLine($"🚻 {loc.Get(lang, "Label_Gender")}: <b>{genderStr}</b> ({loc.Get(lang, "Label_LookingFor")}: <b>{lookingForStr}</b>)");
        sb.AppendLine($"🎯 {loc.Get(lang, "Label_Target")}: <b>{targetStr}</b>");

        if (!string.IsNullOrWhiteSpace(candidate.Greeting))
        {
            sb.AppendLine($"\n💬 <i>\"{candidate.Greeting}\"</i>");
        }

        if (candidate.SelectedInterests.Count > 0)
        {
            var interestTags = string.Join(", ", candidate.SelectedInterests.Select(i => $"{i.Icon} {loc.GetInterestTitle(lang, i.Code.ToString().ToLowerInvariant(), i.Title)}"));
            sb.AppendLine($"\n🏷 <b>{loc.Get(lang, "Label_Interests")}:</b> {interestTags}");
        }

        // Динамическая подсказка в конце анкеты
        var tip = loc.GetRandomSearchTip(lang);
        sb.AppendLine($"\n——\n{tip}");

        Message sentMessage = null!;
        var photoSent = false;
        var candidateText = sb.ToString();
        if (!string.IsNullOrEmpty(candidate.PhotoFileId) && candidateText.Length <= 1024)
        {
            try
            {
                sentMessage = await botClient.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromFileId(candidate.PhotoFileId),
                    caption: candidateText,
                    parseMode: ParseMode.Html,
                    replyMarkup: SearchKeyboards.GetRatingReplyKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                photoSent = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Не удалось отправить фото кандидата {PhotoFileId} пользователю {ChatId}: {ErrorMessage}", candidate.PhotoFileId, chatId, ex.Message);
                sentMessage = null!;
            }
        }

        if (!photoSent)
        {
            sentMessage = await botClient.SendMessage(
                chatId: chatId,
                text: candidateText,
                parseMode: ParseMode.Html,
                replyMarkup: SearchKeyboards.GetRatingReplyKeyboard(lang),
                cancellationToken: cancellationToken
            );
        }

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
    }

    public async Task SendRaterCardAsync(long chatId, UserProfileDto rater, int scoreReceived, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var targetStr = loc.GetDatingTargetText(lang, rater.DatingTarget);
        var genderStr = loc.GetGenderText(lang, rater.Gender);
        var lookingForStr = loc.GetTargetGenderText(lang, rater.TargetGender);

        var nameWithLink = string.IsNullOrEmpty(rater.Username)
            ? $"<a href=\"tg://user?id={rater.TelegramId}\">{rater.Name}</a>"
            : $"<a href=\"tg://user?id={rater.TelegramId}\">{rater.Name}</a> (@{rater.Username})";

        var sb = new StringBuilder();
        sb.AppendLine(loc.Get(lang, "Notification_RatingScoreReceived", scoreReceived));
        sb.AppendLine($"👤 <b>{nameWithLink}</b>, {rater.Age}");
        sb.AppendLine($"📍 {loc.Get(lang, "Label_City")}: <b>{rater.City}</b>");
        if (rater.Height.HasValue)
        {
            sb.AppendLine($"📏 {loc.Get(lang, "Label_Height")}: <b>{rater.Height} cm</b>");
        }
        sb.AppendLine($"🚻 {loc.Get(lang, "Label_Gender")}: <b>{genderStr}</b> ({loc.Get(lang, "Label_LookingFor")}: <b>{lookingForStr}</b>)");
        sb.AppendLine($"🎯 {loc.Get(lang, "Label_Target")}: <b>{targetStr}</b>");

        if (!string.IsNullOrWhiteSpace(rater.Greeting))
        {
            sb.AppendLine($"\n💬 <i>\"{rater.Greeting}\"</i>");
        }

        if (rater.SelectedInterests.Count > 0)
        {
            var interestTags = string.Join(", ", rater.SelectedInterests.Select(i => $"{i.Icon} {loc.GetInterestTitle(lang, i.Code.ToString().ToLowerInvariant(), i.Title)}"));
            sb.AppendLine($"\n🏷 <b>{loc.Get(lang, "Label_Interests")}:</b> {interestTags}");
        }

        Message sentMessage = null!;
        var photoSent = false;
        var raterText = sb.ToString();
        var ratingReplyKeyboard = SearchKeyboards.GetIncomingRatingReplyKeyboard(lang);
        if (!string.IsNullOrEmpty(rater.PhotoFileId) && raterText.Length <= 1024)
        {
            try
            {
                sentMessage = await botClient.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromFileId(rater.PhotoFileId),
                    caption: raterText,
                    parseMode: ParseMode.Html,
                    replyMarkup: ratingReplyKeyboard,
                    cancellationToken: cancellationToken
                );
                photoSent = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Не удалось отправить фото оценившего {PhotoFileId} пользователю {ChatId}: {ErrorMessage}", rater.PhotoFileId, chatId, ex.Message);
                sentMessage = null!;
            }
        }

        if (!photoSent)
        {
            sentMessage = await botClient.SendMessage(
                chatId: chatId,
                text: raterText,
                parseMode: ParseMode.Html,
                replyMarkup: ratingReplyKeyboard,
                cancellationToken: cancellationToken
            );
        }

        Message followUpMessage = null!;
        var inlineKeyboard = SearchKeyboards.GetRaterCardKeyboard(rater.TelegramId, rater.Username, lang);
        try
        {
            followUpMessage = await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Notification_CanMessageUser"),
                parseMode: ParseMode.Html,
                replyMarkup: inlineKeyboard,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning("Не удалось отправить сообщение с кнопкой 'Написать' пользователю {ChatId}: {ErrorMessage}", chatId, ex.Message);
        }

        var lastMessageId = followUpMessage?.MessageId ?? sentMessage.MessageId;
        await registrationService.SaveLastBotMessageIdAsync(chatId, lastMessageId, cancellationToken);
    }

    public async Task SendReportReasonsPromptAsync(long chatId, Guid candidateProfileId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        await botClient.SendMessage(
            chatId: chatId,
            text: $"🚨 <b>{loc.Get(lang, "Btn_Report")}</b>",
            parseMode: ParseMode.Html,
            replyMarkup: SearchKeyboards.GetReportReasonsKeyboard(candidateProfileId, lang),
            cancellationToken: cancellationToken
        );
    }

    public async Task SendCustomReasonPromptAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        await botClient.SendMessage(
            chatId: chatId,
            text: loc.Get(lang, "Prompt_ReportDetails"),
            parseMode: ParseMode.Html,
            replyMarkup: SearchKeyboards.GetCancelReportReplyKeyboard(lang),
            cancellationToken: cancellationToken
        );
    }

    public async Task SendMutualMatchNotificationAsync(long recipientTelegramId, UserProfileDto partner, int myScore, int partnerScore, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userRepository.GetByTelegramIdAsync(recipientTelegramId, cancellationToken);
            var lang = user?.Language ?? AppLanguage.Russian;

            var partnerLink = string.IsNullOrEmpty(partner.Username)
                ? $"<a href=\"tg://user?id={partner.TelegramId}\">{partner.Name}</a>"
                : $"<a href=\"tg://user?id={partner.TelegramId}\">{partner.Name}</a> (@{partner.Username})";

            var contactLink = string.IsNullOrEmpty(partner.Username)
                ? $"<a href=\"tg://user?id={partner.TelegramId}\">{partner.Name}</a>"
                : $"@{partner.Username}";

            var targetStr = loc.GetDatingTargetText(lang, partner.DatingTarget);

            var sb = new StringBuilder();
            sb.AppendLine($"{loc.Get(lang, "Notification_MutualMatch")}\n");
            sb.AppendLine($"👤 <b>{partnerLink}</b> ({partnerScore}/10 ⭐)");
            sb.AppendLine(loc.Get(lang, "Notification_MutualScore", myScore));
            sb.AppendLine(loc.Get(lang, "Notification_MutualContact", contactLink));
            sb.AppendLine($"👤 {partner.Name}, {partner.Age}");
            sb.AppendLine($"📍 {partner.City}");
            if (partner.Height.HasValue) sb.AppendLine($"📏 {partner.Height} cm");
            sb.AppendLine($"🎯 {targetStr}");

            if (!string.IsNullOrWhiteSpace(partner.Greeting))
            {
                sb.AppendLine($"💬 <i>\"{partner.Greeting}\"</i>");
            }

            if (partner.SelectedInterests.Count > 0)
            {
                var interestTags = string.Join(", ", partner.SelectedInterests.Select(i => $"{i.Icon} {loc.GetInterestTitle(lang, i.Code.ToString().ToLowerInvariant(), i.Title)}"));
                sb.AppendLine($"🏷 {interestTags}");
            }

            var photoSent = false;
            var matchText = sb.ToString();
            var keyboard = SearchKeyboards.GetMutualMatchKeyboard(partner.TelegramId, partner.Username, lang);
            if (!string.IsNullOrEmpty(partner.PhotoFileId) && matchText.Length <= 1024)
            {
                try
                {
                    await botClient.SendPhoto(
                        chatId: recipientTelegramId,
                        photo: InputFile.FromFileId(partner.PhotoFileId),
                        caption: matchText,
                        parseMode: ParseMode.Html,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken
                    );
                    photoSent = true;
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Не удалось отправить фото при взаимной симпатии {PhotoFileId} пользователю {TelegramId}: {ErrorMessage}", partner.PhotoFileId, recipientTelegramId, ex.Message);
                }
            }

            if (!photoSent)
            {
                await botClient.SendMessage(
                    chatId: recipientTelegramId,
                    text: matchText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Не удалось отправить уведомление о взаимной симпатии пользователю {TelegramId}: {ErrorMessage}", recipientTelegramId, ex.Message);
        }
    }

    public async Task SendHighRatingNotificationAsync(long targetTelegramId, Guid ratingId, int score, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userRepository.GetByTelegramIdAsync(targetTelegramId, cancellationToken);
            var lang = user?.Language ?? AppLanguage.Russian;

            await botClient.SendMessage(
                chatId: targetTelegramId,
                text: loc.Get(lang, "Notification_HighRating", score),
                parseMode: ParseMode.Html,
                replyMarkup: SearchKeyboards.GetIncomingRatingNotificationKeyboard(ratingId, lang),
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning("Не удалось отправить уведомление об оценке пользователю {TelegramId}: {ErrorMessage}", targetTelegramId, ex.Message);
        }
    }

    public async Task SendAlreadyRatedRecentlyNotificationAsync(long recipientTelegramId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userRepository.GetByTelegramIdAsync(recipientTelegramId, cancellationToken);
            var lang = user?.Language ?? AppLanguage.Russian;

            await botClient.SendMessage(
                chatId: recipientTelegramId,
                text: loc.Get(lang, "Notification_AlreadyRatedRecently"),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning("Не удалось отправить уведомление о недавней оценке пользователю {TelegramId}: {ErrorMessage}", recipientTelegramId, ex.Message);
        }
    }

    public async Task SendNoCandidatesMessageAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: loc.Get(lang, "SearchEmpty"),
            parseMode: ParseMode.Html,
            replyMarkup: SearchKeyboards.GetNoCandidatesKeyboard(lang),
            cancellationToken: cancellationToken
        );

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
    }

    public async Task SendReportToAdminsAsync(ReportInfo report, CancellationToken cancellationToken = default)
    {
        var adminIds = configuration.GetSection("BotConfiguration:AdminIds").Get<List<long>>() ?? [];
        if (adminIds.Count == 0)
        {
            logger.LogInformation("Администраторы для отправки жалобы не настроены в BotConfiguration:AdminIds.");
            return;
        }

        var targetStr = loc.GetDatingTargetText(AppLanguage.Russian, report.ReportedProfile.DatingTarget);
        var genderStr = report.ReportedProfile.Gender == Gender.Male ? "Парень 👦" : "Девушка 👧";
        var lookingForStr = report.ReportedProfile.TargetGender switch
        {
            TargetGender.Male => "Парня 👦",
            TargetGender.Female => "Девушку 👧",
            TargetGender.All => "Всех 👥",
            _ => "Любого"
        };

        var ratingStr = report.ReportedProfile.RatingCount > 0
            ? $"⭐ {report.ReportedProfile.AverageRating:F1} / 10 (оценок: {report.ReportedProfile.RatingCount})"
            : "⭐ Пока нет оценок";

        // Сообщение №1: Полная анкета нарушителя (как в "Мой профиль")
        var cardSb = new StringBuilder();
        cardSb.AppendLine("📋 <b>Анкета пользователя, на которого пожаловались:</b>\n");
        cardSb.AppendLine($"🏷 <b>Имя:</b> {report.ReportedProfile.Name}");
        cardSb.AppendLine($"🎂 <b>Возраст:</b> {report.ReportedProfile.Age}");
        cardSb.AppendLine($"📍 <b>Город:</b> {report.ReportedProfile.City}");
        cardSb.AppendLine($"📏 <b>Рост:</b> {(report.ReportedProfile.Height.HasValue ? $"{report.ReportedProfile.Height} см" : "не указан")}");
        cardSb.AppendLine($"🚻 <b>Пол:</b> {genderStr}");
        cardSb.AppendLine($"🔍 <b>Кого ищу:</b> {lookingForStr}");
        cardSb.AppendLine($"🎯 <b>Цель знакомства:</b> {targetStr}");
        cardSb.AppendLine($"📊 <b>Рейтинг анкеты:</b> {ratingStr}");

        if (report.ReportedProfile.SelectedInterests.Count > 0)
        {
            var interestTags = string.Join(", ", report.ReportedProfile.SelectedInterests.Select(i => $"{i.Icon} {i.Title}"));
            cardSb.AppendLine($"🏷 <b>Интересы:</b> {interestTags}");
        }

        if (!string.IsNullOrWhiteSpace(report.ReportedProfile.AiDescription))
        {
            cardSb.AppendLine($"\n🧠 <b>Скрытое описание для ИИ:</b>\n<i>\"{report.ReportedProfile.AiDescription}\"</i>");
        }

        if (!string.IsNullOrWhiteSpace(report.ReportedProfile.Greeting))
        {
            cardSb.AppendLine($"\n💬 <b>Приветствие:</b>\n<i>\"{report.ReportedProfile.Greeting}\"</i>");
        }

        // Сообщение №2: Данные о жалобе и заявителе
        var reasonStr = report.Reason switch
        {
            ReportReason.InappropriateContent => "🔞 Запрещенный / непристойный контент",
            ReportReason.IncorrectProfile => "📑 Некорректная / фейковая анкета",
            ReportReason.Other => "❓ Другое",
            _ => "Не указана"
        };

        var reporterTag = string.IsNullOrEmpty(report.ReporterUsername)
            ? $"<a href=\"tg://user?id={report.ReporterTelegramId}\">{report.ReporterFirstName ?? "Пользователь"}</a> (ID: <code>{report.ReporterTelegramId}</code>)"
            : $"<a href=\"tg://user?id={report.ReporterTelegramId}\">{report.ReporterFirstName ?? "Пользователь"}</a> (@{report.ReporterUsername}, ID: <code>{report.ReporterTelegramId}</code>)";

        var reportedTag = string.IsNullOrEmpty(report.ReportedProfile.Username)
            ? $"<a href=\"tg://user?id={report.ReportedProfile.TelegramId}\">{report.ReportedProfile.Name}</a> (ID: <code>{report.ReportedProfile.TelegramId}</code>)"
            : $"<a href=\"tg://user?id={report.ReportedProfile.TelegramId}\">{report.ReportedProfile.Name}</a> (@{report.ReportedProfile.Username}, ID: <code>{report.ReportedProfile.TelegramId}</code>)";

        var reportSb = new StringBuilder();
        reportSb.AppendLine("🚨 <b>ЖАЛОБА НА АНКЕТУ</b>\n");
        reportSb.AppendLine($"👤 <b>От кого:</b> {reporterTag}");
        reportSb.AppendLine($"👤 <b>На кого:</b> {reportedTag}");
        reportSb.AppendLine($"📌 <b>Причина жалобы:</b> <b>{reasonStr}</b>");
        if (!string.IsNullOrWhiteSpace(report.Details))
        {
            reportSb.AppendLine($"\n📝 <b>Комментарий заявителя:</b>\n<i>\"{report.Details}\"</i>");
        }

        var cardText = cardSb.ToString();
        var reportText = reportSb.ToString();

        foreach (var adminId in adminIds)
        {
            try
            {
                var photoSent = false;
                if (!string.IsNullOrEmpty(report.ReportedProfile.PhotoFileId) && cardText.Length <= 1024)
                {
                    try
                    {
                        await botClient.SendPhoto(
                            chatId: adminId,
                            photo: InputFile.FromFileId(report.ReportedProfile.PhotoFileId),
                            caption: cardText,
                            parseMode: ParseMode.Html,
                            cancellationToken: cancellationToken
                        );
                        photoSent = true;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Не удалось отправить фото нарушителя {PhotoFileId} админу {AdminId}: {ErrorMessage}", report.ReportedProfile.PhotoFileId, adminId, ex.Message);
                    }
                }

                if (!photoSent)
                {
                    await botClient.SendMessage(
                        chatId: adminId,
                        text: cardText,
                        parseMode: ParseMode.Html,
                        cancellationToken: cancellationToken
                    );
                }

                await botClient.SendMessage(
                    chatId: adminId,
                    text: reportText,
                    parseMode: ParseMode.Html,
                    replyMarkup: SearchKeyboards.GetAdminModerationKeyboard(report.ReportId),
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning("Не удалось отправить отчет о жалобе администратору {AdminId}: {ErrorMessage}", adminId, ex.Message);
            }
        }
    }
}
