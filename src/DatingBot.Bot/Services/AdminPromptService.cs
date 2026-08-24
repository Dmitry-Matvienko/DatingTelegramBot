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

public class AdminPromptService(
    ITelegramBotClient botClient,
    IAdminService adminService,
    IRegistrationService registrationService,
    IUserRepository userRepository,
    ILocalizationService loc,
    ILogger<AdminPromptService> logger)
{
    public async Task SendAdminWelcomeAsync(long chatId, int? prevMessageId = null, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        await DeleteMessageSafeAsync(chatId, prevMessageId, cancellationToken);

        var sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: loc.Get(lang, "Admin_Welcome"),
            parseMode: ParseMode.Html,
            replyMarkup: MainMenuKeyboards.GetMainMenuReplyKeyboard(lang),
            cancellationToken: cancellationToken
        );

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
    }

    public async Task SendAdminPanelAsync(long chatId, int? prevMessageId = null, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        await DeleteMessageSafeAsync(chatId, prevMessageId, cancellationToken);

        var pendingReportsCount = await adminService.GetPendingReportsCountAsync(cancellationToken);

        var sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: loc.Get(lang, "Admin_Panel_Title"),
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboards.GetAdminPanelKeyboard(pendingReportsCount, lang),
            cancellationToken: cancellationToken
        );

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
    }

    public async Task SendAdminStatsAsync(long chatId, int? prevMessageId = null, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        await DeleteMessageSafeAsync(chatId, prevMessageId, cancellationToken);

        var stats = await adminService.GetOverallStatsAsync(cancellationToken);

        var totalTargetSum = stats.DatingTargetFriendsCount + stats.DatingTargetRelationshipCount + stats.DatingTargetAdultOnlyCount;
        var friendsPct = totalTargetSum > 0 ? (stats.DatingTargetFriendsCount * 100.0 / totalTargetSum) : 0;
        var relPct = totalTargetSum > 0 ? (stats.DatingTargetRelationshipCount * 100.0 / totalTargetSum) : 0;
        var adultPct = totalTargetSum > 0 ? (stats.DatingTargetAdultOnlyCount * 100.0 / totalTargetSum) : 0;

        var totalGenderSum = stats.MaleCount + stats.FemaleCount;
        var malePct = totalGenderSum > 0 ? (stats.MaleCount * 100.0 / totalGenderSum) : 0;
        var femalePct = totalGenderSum > 0 ? (stats.FemaleCount * 100.0 / totalGenderSum) : 0;

        var sb = new StringBuilder();
        sb.AppendLine("📊 <b>МЕДИАКИТ И СТАТИСТИКА АУДИТОРИИ</b>\n");
        sb.AppendLine($"👥 <b>Всего пользователей:</b> <b>{stats.TotalUsers:N0}</b>");
        sb.AppendLine($"📋 <b>Заполненных анкет:</b> <b>{stats.CompletedProfiles:N0}</b>");
        sb.AppendLine($"🚫 <b>Заблокированных:</b> <b>{stats.BannedUsers:N0}</b>\n");

        sb.AppendLine("🚻 <b>Гендерный срез:</b>");
        sb.AppendLine($"• 👦 Парни: <b>{stats.MaleCount:N0}</b> ({malePct:F1}%)");
        sb.AppendLine($"• 👧 Девушки: <b>{stats.FemaleCount:N0}</b> ({femalePct:F1}%)\n");

        sb.AppendLine("📈 <b>Динамика регистраций:</b>");
        sb.AppendLine($"• За 24 часа: <b>+{stats.NewUsersLast24Hours:N0}</b>");
        sb.AppendLine($"• За 7 дней: <b>+{stats.NewUsersLast7Days:N0}</b>");
        sb.AppendLine($"• За 30 дней: <b>+{stats.NewUsersLast30Days:N0}</b>\n");

        sb.AppendLine("🎯 <b>Цели аудитории (количество и %):</b>");
        sb.AppendLine($"• 👥 Общение и друзья: <b>{stats.DatingTargetFriendsCount:N0}</b> ({friendsPct:F1}%)");
        sb.AppendLine($"• ❤️ Отношения: <b>{stats.DatingTargetRelationshipCount:N0}</b> ({relPct:F1}%)");
        sb.AppendLine($"• 🔞 18+: <b>{stats.DatingTargetAdultOnlyCount:N0}</b> ({adultPct:F1}%)\n");

        sb.AppendLine("🎂 <b>Возрастной срез:</b>");
        sb.AppendLine($"• До 18 лет: <b>{stats.AgeUnder18Count:N0}</b>");
        sb.AppendLine($"• 18–24 года: <b>{stats.Age18To24Count:N0}</b>");
        sb.AppendLine($"• 25–34 года: <b>{stats.Age25To34Count:N0}</b>");
        sb.AppendLine($"• 35–44 года: <b>{stats.Age35To44Count:N0}</b>");
        sb.AppendLine($"• 45+ лет: <b>{stats.Age45PlusCount:N0}</b>\n");

        if (stats.TopCities.Count > 0)
        {
            sb.AppendLine("🏙 <b>Топ городов по пользователям:</b>");
            for (var i = 0; i < stats.TopCities.Count; i++)
            {
                var c = stats.TopCities[i];
                var countrySuffix = !string.IsNullOrEmpty(c.Country) ? $", {c.Country}" : "";
                sb.AppendLine($"{i + 1}. <b>{c.CityName}{countrySuffix}</b> — {c.UserCount:N0} чел. (👦 {c.MaleCount} | 👧 {c.FemaleCount})");
            }
            sb.AppendLine();
        }

        if (stats.TopCountries.Count > 0)
        {
            sb.AppendLine("🌍 <b>Топ стран:</b>");
            for (var i = 0; i < stats.TopCountries.Count; i++)
            {
                var country = stats.TopCountries[i];
                sb.AppendLine($"{i + 1}. <b>{country.CountryName}</b> — {country.UserCount:N0} чел.");
            }
        }

        var sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: sb.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboards.GetAdminStatsKeyboard(lang),
            cancellationToken: cancellationToken
        );

        await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
    }

    public async Task SendCityStatsAsync(long chatId, AdminCityStatsDto cityStats, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var totalGender = cityStats.MaleCount + cityStats.FemaleCount;
        var malePct = totalGender > 0 ? (cityStats.MaleCount * 100.0 / totalGender) : 0;
        var femalePct = totalGender > 0 ? (cityStats.FemaleCount * 100.0 / totalGender) : 0;

        var countryStr = !string.IsNullOrEmpty(cityStats.Country) ? cityStats.Country : "—";
        var text = loc.Get(lang, "Admin_Stats_CityResult", cityStats.CityName, countryStr, cityStats.UserCount, cityStats.CompletedCount, cityStats.MaleCount, $"{malePct:F1}", cityStats.FemaleCount, $"{femalePct:F1}");

        await botClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboards.GetBackToStatsKeyboard(lang),
            cancellationToken: cancellationToken
        );
    }

    public async Task SendAdminCandidateCardAsync(long chatId, UserProfileDto candidate, Gender gender, int currentIndex, int totalCount, int nextOffset, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var targetStr = loc.GetDatingTargetText(lang, candidate.DatingTarget);
        var genderStr = loc.GetGenderText(lang, candidate.Gender);
        var lookingForStr = loc.GetTargetGenderText(lang, candidate.TargetGender);

        var userAccountLink = string.IsNullOrEmpty(candidate.Username)
            ? $"<a href=\"tg://user?id={candidate.TelegramId}\">{candidate.Name}</a>"
            : $"<a href=\"tg://user?id={candidate.TelegramId}\">{candidate.Name}</a> (@{candidate.Username})";

        var ratingStr = candidate.RatingCount > 0
            ? $"⭐ {candidate.AverageRating:F1} / 10 (оценок: {candidate.RatingCount})"
            : "⭐ Пока нет оценок";

        var sb = new StringBuilder();
        sb.AppendLine($"📋 <b>Анкета #{currentIndex} из {totalCount}</b>\n");
        sb.AppendLine($"👤 <b>Пользователь:</b> {userAccountLink} (ID: <code>{candidate.TelegramId}</code>)");
        sb.AppendLine($"🏷 <b>Имя:</b> {candidate.Name}");
        sb.AppendLine($"🎂 <b>Возраст:</b> {candidate.Age}");
        sb.AppendLine($"📍 <b>Город:</b> {candidate.City}");
        sb.AppendLine($"📏 <b>Рост:</b> {(candidate.Height.HasValue ? $"{candidate.Height} см" : "не указан")}");
        sb.AppendLine($"🚻 <b>Пол:</b> {genderStr} (ищет: {lookingForStr})");
        sb.AppendLine($"🎯 <b>Цель знакомства:</b> {targetStr}");
        sb.AppendLine($"📊 <b>Рейтинг:</b> {ratingStr}");

        if (candidate.SelectedInterests.Count > 0)
        {
            var interestTags = string.Join(", ", candidate.SelectedInterests.Select(i => $"{i.Icon} {loc.GetInterestTitle(lang, i.Code.ToString().ToLowerInvariant(), i.Title)}"));
            sb.AppendLine($"\n🏷 <b>Интересы:</b> {interestTags}");
        }

        if (!string.IsNullOrWhiteSpace(candidate.AiDescription))
        {
            sb.AppendLine($"\n🧠 <b>Скрытое описание для ИИ:</b>\n<i>\"{candidate.AiDescription}\"</i>");
        }

        if (!string.IsNullOrWhiteSpace(candidate.Greeting))
        {
            sb.AppendLine($"\n💬 <b>Приветствие:</b>\n<i>\"{candidate.Greeting}\"</i>");
        }

        var keyboard = AdminKeyboards.GetAdminProfileCardKeyboard(candidate.Id, candidate.TelegramId, candidate.Username, gender, nextOffset, lang);

        var photoSent = false;
        var cardText = sb.ToString();
        if (!string.IsNullOrEmpty(candidate.PhotoFileId) && cardText.Length <= 1024)
        {
            try
            {
                await botClient.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromFileId(candidate.PhotoFileId),
                    caption: cardText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
                photoSent = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Не удалось отправить фото профиля {PhotoFileId} в админке пользователю {ChatId}: {ErrorMessage}", candidate.PhotoFileId, chatId, ex.Message);
            }
        }

        if (!photoSent)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: cardText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task SendPendingReportCardAsync(long chatId, AdminPendingReportDto report, int currentIndex, int totalCount, int nextSkip, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var targetStr = loc.GetDatingTargetText(lang, report.ReportedProfile.DatingTarget);
        var genderStr = loc.GetGenderText(lang, report.ReportedProfile.Gender);
        var lookingForStr = loc.GetTargetGenderText(lang, report.ReportedProfile.TargetGender);

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

        var cardSb = new StringBuilder();
        cardSb.AppendLine($"🚨 <b>ЖАЛОБА #{currentIndex} из {totalCount}</b>\n");
        cardSb.AppendLine($"👤 <b>Заявитель:</b> {reporterTag}");
        cardSb.AppendLine($"👤 <b>Нарушитель:</b> {reportedTag}");
        cardSb.AppendLine($"📌 <b>Причина:</b> <b>{reasonStr}</b>");
        if (!string.IsNullOrWhiteSpace(report.Details))
        {
            cardSb.AppendLine($"📝 <b>Комментарий заявителя:</b> <i>\"{report.Details}\"</i>");
        }
        cardSb.AppendLine("\n📋 <b>Данные анкеты нарушителя:</b>");
        cardSb.AppendLine($"🏷 <b>Имя:</b> {report.ReportedProfile.Name}, <b>Возраст:</b> {report.ReportedProfile.Age}");
        cardSb.AppendLine($"📍 <b>Город:</b> {report.ReportedProfile.City}");
        cardSb.AppendLine($"🚻 <b>Пол:</b> {genderStr} (ищет: {lookingForStr})");
        cardSb.AppendLine($"🎯 <b>Цель:</b> {targetStr}");

        if (!string.IsNullOrWhiteSpace(report.ReportedProfile.AiDescription))
        {
            cardSb.AppendLine($"\n🧠 <b>Скрытое описание для ИИ:</b>\n<i>\"{report.ReportedProfile.AiDescription}\"</i>");
        }

        if (!string.IsNullOrWhiteSpace(report.ReportedProfile.Greeting))
        {
            cardSb.AppendLine($"\n💬 <b>Приветствие:</b>\n<i>\"{report.ReportedProfile.Greeting}\"</i>");
        }

        var keyboard = AdminKeyboards.GetAdminPendingReportKeyboard(report.ReportId, nextSkip, totalCount, lang);

        var reportPhotoSent = false;
        var reportCardText = cardSb.ToString();
        if (!string.IsNullOrEmpty(report.ReportedProfile.PhotoFileId) && reportCardText.Length <= 1024)
        {
            try
            {
                await botClient.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromFileId(report.ReportedProfile.PhotoFileId),
                    caption: reportCardText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
                reportPhotoSent = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Не удалось отправить фото нарушителя {PhotoFileId} админу {ChatId}: {ErrorMessage}", report.ReportedProfile.PhotoFileId, chatId, ex.Message);
            }
        }

        if (!reportPhotoSent)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: reportCardText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task SendBroadcastPreviewAsync(long chatId, AdminBroadcastPreviewDto preview, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var reachText = loc.Get(lang, "Admin_Broadcast_Preview_Caption", preview.TargetRecipientCount);

        var confirmKeyboard = AdminKeyboards.GetAdminBroadcastConfirmKeyboard(preview.ButtonText, preview.ButtonUrl, lang);

        var previewPhotoSent = false;
        var captionText = $"{preview.Text}\n\n{reachText}";
        if (!string.IsNullOrEmpty(preview.PhotoFileId) && captionText.Length <= 1024)
        {
            try
            {
                await botClient.SendPhoto(
                    chatId: chatId,
                    photo: InputFile.FromFileId(preview.PhotoFileId),
                    caption: captionText,
                    parseMode: ParseMode.Html,
                    replyMarkup: confirmKeyboard,
                    cancellationToken: cancellationToken
                );
                previewPhotoSent = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Не удалось отправить фото предпросмотра рассылки {PhotoFileId} админу {ChatId}: {ErrorMessage}", preview.PhotoFileId, chatId, ex.Message);
            }
        }

        if (!previewPhotoSent)
        {
            await botClient.SendMessage(
                chatId: chatId,
                text: captionText,
                parseMode: ParseMode.Html,
                replyMarkup: confirmKeyboard,
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task SendAdminRevenueMenuAsync(long chatId, int? prevMessageId = null, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        await DeleteMessageSafeAsync(chatId, prevMessageId, cancellationToken);

        var sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: loc.Get(lang, "Admin_Revenue_Menu"),
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboards.GetAdminRevenueKeyboard(lang),
            cancellationToken: cancellationToken
        );

        if (sentMessage is not null)
        {
            await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
        }
    }

    public async Task SendAdminBalanceReportAsync(long chatId, int? prevMessageId = null, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        await DeleteMessageSafeAsync(chatId, prevMessageId, cancellationToken);

        var stats = await adminService.GetRevenueStatsAsync(cancellationToken);
        var usdEquivalent = stats.TotalEarnedStars * 0.013;

        var text = string.Format(
            loc.Get(lang, "Admin_Revenue_Balance_Report"),
            stats.TotalEarnedStars,
            usdEquivalent,
            stats.TotalTransactionsCount,
            stats.EarnedLast24Hours,
            stats.EarnedLast7Days,
            stats.EarnedLast30Days
        );

        var sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboards.GetAdminRevenueDetailsKeyboard(lang, isBalanceScreen: true),
            cancellationToken: cancellationToken
        );

        if (sentMessage is not null)
        {
            await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
        }
    }

    public async Task SendAdminTransactionHistoryAsync(long chatId, int? prevMessageId = null, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(chatId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        await DeleteMessageSafeAsync(chatId, prevMessageId, cancellationToken);

        var txs = await adminService.GetRecentTransactionsAsync(20, cancellationToken);

        var sb = new StringBuilder();
        sb.Append(string.Format(loc.Get(lang, "Admin_Revenue_History_Header"), txs.Count > 0 ? txs.Count : 20));

        if (txs.Count == 0)
        {
            sb.AppendLine(loc.Get(lang, "Admin_Revenue_NoTransactions"));
        }
        else
        {
            for (var i = 0; i < txs.Count; i++)
            {
                var t = txs[i];
                var displayName = !string.IsNullOrWhiteSpace(t.FirstName) ? t.FirstName : (!string.IsNullOrWhiteSpace(t.Username) ? $"@{t.Username}" : $"ID:{t.TelegramId}");
                var safeName = System.Net.WebUtility.HtmlEncode(displayName);
                var purpose = t.Type switch
                {
                    PaymentType.Unban => "Разблокировка аккаунта",
                    PaymentType.Subscription => "Премиум подписка",
                    _ => "Оплата услуг"
                };

                sb.AppendLine($"<b>#{i + 1}</b> | 📅 <code>{t.CreatedAt:dd.MM.yyyy HH:mm}</code> UTC");
                sb.AppendLine($"   👤 <b>Пользователь:</b> <a href=\"tg://user?id={t.TelegramId}\">{safeName}</a> (<code>{t.TelegramId}</code>)");
                sb.AppendLine($"   ⭐️ <b>Сумма:</b> <code>+{t.Amount} ⭐ {t.Currency}</code>");
                sb.AppendLine($"   🎯 <b>Назначение:</b> <i>{purpose}</i>");
                if (!string.IsNullOrWhiteSpace(t.TelegramPaymentChargeId))
                {
                    sb.AppendLine($"   🧾 <b>ID платежа:</b> <code>{t.TelegramPaymentChargeId}</code>");
                }
                sb.AppendLine();
            }
        }

        var sentMessage = await botClient.SendMessage(
            chatId: chatId,
            text: sb.ToString().TrimEnd(),
            parseMode: ParseMode.Html,
            replyMarkup: AdminKeyboards.GetAdminRevenueDetailsKeyboard(lang, isBalanceScreen: false),
            cancellationToken: cancellationToken
        );

        if (sentMessage is not null)
        {
            await registrationService.SaveLastBotMessageIdAsync(chatId, sentMessage.MessageId, cancellationToken);
        }
    }

    public async Task DeleteMessageSafeAsync(long chatId, int? messageId, CancellationToken cancellationToken = default)
    {
        if (!messageId.HasValue) return;

        try
        {
            await botClient.DeleteMessage(chatId, messageId.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Не удалось удалить сообщение {MessageId} в чате {ChatId}", messageId.Value, chatId);
        }
    }
}
