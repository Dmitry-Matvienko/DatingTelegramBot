using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Bot.Keyboards;
using DatingBot.Bot.Services;
using DatingBot.Domain.Enums;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using DbUser = DatingBot.Domain.Entities.User;

namespace DatingBot.Bot.Handlers;

public class AdminCallbackHandler(
    ITelegramBotClient botClient,
    IAdminService adminService,
    IModerationService moderationService,
    AdminPromptService adminPromptService,
    AdminBroadcastService adminBroadcastService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILocalizationService loc,
    ILogger<AdminCallbackHandler> logger)
{
    public async Task<bool> HandleAdminCallbackQueryAsync(DbUser user, CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
    {
        var data = callbackQuery.Data ?? string.Empty;
        if (!data.StartsWith("adm_"))
        {
            return false;
        }

        if (!adminService.IsAdmin(callbackQuery.From.Id))
        {
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                loc.Get(user.Language, "Admin_Alert_NoAccess"),
                showAlert: true,
                cancellationToken: cancellationToken
            );
            return true;
        }

        var lang = user.Language;
        var chatId = callbackQuery.Message?.Chat.Id ?? callbackQuery.From.Id;

        // 1. Навигация по панели управления: adm_panel:*
        if (data.StartsWith("adm_panel:"))
        {
            var action = data["adm_panel:".Length..];
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);

            if (action == "main")
            {
                user.State = UserState.Admin_Panel;
                userRepository.Update(user);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await adminPromptService.SendAdminPanelAsync(chatId, callbackQuery.Message?.MessageId, cancellationToken);
                return true;
            }

            if (action == "stats")
            {
                user.State = UserState.Admin_Panel;
                userRepository.Update(user);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await adminPromptService.SendAdminStatsAsync(chatId, callbackQuery.Message?.MessageId, cancellationToken);
                return true;
            }

            if (action == "bcast")
            {
                adminBroadcastService.ClearSession(callbackQuery.From.Id);
                user.State = UserState.Admin_Panel;
                userRepository.Update(user);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "Admin_Broadcast_Menu"),
                    parseMode: ParseMode.Html,
                    replyMarkup: AdminKeyboards.GetAdminBroadcastAudienceKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                return true;
            }

            if (action == "reports")
            {
                var reports = await adminService.GetPendingReportsAsync(0, 1, cancellationToken);
                var totalReports = await adminService.GetPendingReportsCountAsync(cancellationToken);

                if (reports.Count == 0)
                {
                    await botClient.AnswerCallbackQuery(
                        callbackQuery.Id,
                        loc.Get(lang, "Admin_Reports_NoPending"),
                        showAlert: true,
                        cancellationToken: cancellationToken
                    );
                    return true;
                }

                await adminPromptService.SendPendingReportCardAsync(chatId, reports[0], 1, totalReports, 1, cancellationToken);
                return true;
            }

            if (action == "lang")
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "LanguagePrompt"),
                    parseMode: ParseMode.Html,
                    replyMarkup: LanguageKeyboards.GetLanguageSelectionKeyboard("edit_lang"),
                    cancellationToken: cancellationToken
                );
                return true;
            }
        }

        // 2. Статистика по городу: adm_stats:city_search
        if (data == "adm_stats:city_search")
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            user.State = UserState.Admin_Stats_WaitingForCity;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Stats_CityPrompt"),
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboards.GetBackToStatsKeyboard(lang),
                cancellationToken: cancellationToken
            );
            return true;
        }

        // 3. Рассылка: выбор аудитории (adm_bcast:all / adm_bcast:target)
        if (data == "adm_bcast:all")
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            var session = adminBroadcastService.GetOrCreateSession(callbackQuery.From.Id);
            session.Filter = new AdminBroadcastFilterDto();
            session.CalculatedReach = await adminService.GetBroadcastAudienceCountAsync(session.Filter, cancellationToken);

            user.State = UserState.Admin_Broadcasting_WaitingForContent;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Broadcast_Content_Prompt"),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
            return true;
        }

        if (data == "adm_bcast:target")
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            var session = adminBroadcastService.GetOrCreateSession(callbackQuery.From.Id);
            session.Filter = new AdminBroadcastFilterDto();

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Broadcast_Gender_Prompt"),
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboards.GetAdminBroadcastGenderKeyboard(lang),
                cancellationToken: cancellationToken
            );
            return true;
        }

        if (data.StartsWith("adm_bgender:") || data.StartsWith("adm_btarget:"))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            var choice = data.StartsWith("adm_bgender:")
                ? data["adm_bgender:".Length..]
                : data["adm_btarget:".Length..];

            var session = adminBroadcastService.GetOrCreateSession(callbackQuery.From.Id);

            session.Filter = choice switch
            {
                "male" => session.Filter with { TargetGender = Gender.Male, TargetGoal = null },
                "female" => session.Filter with { TargetGender = Gender.Female, TargetGoal = null },
                "friends" => session.Filter with { TargetGender = null, TargetGoal = DatingTarget.Friends },
                "relationship" => session.Filter with { TargetGender = null, TargetGoal = DatingTarget.Relationship },
                "adult" => session.Filter with { TargetGender = null, TargetGoal = DatingTarget.AdultOnly },
                _ => session.Filter with { TargetGender = null, TargetGoal = null }
            };

            user.State = UserState.Admin_Broadcasting_WaitingForCity;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Broadcast_City_Prompt"),
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboards.GetAdminBroadcastSkipCityKeyboard(lang),
                cancellationToken: cancellationToken
            );
            return true;
        }

        if (data == "adm_bcity:skip")
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            var session = adminBroadcastService.GetOrCreateSession(callbackQuery.From.Id);
            session.Filter = session.Filter with { City = null };

            user.State = UserState.Admin_Broadcasting_WaitingForContent;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Broadcast_Content_Prompt"),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
            return true;
        }

        if (data == "adm_bbtn:skip")
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            var session = adminBroadcastService.GetOrCreateSession(callbackQuery.From.Id);
            session.ButtonText = null;
            session.ButtonUrl = null;

            user.State = UserState.Admin_Panel;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            session.CalculatedReach = await adminService.GetBroadcastAudienceCountAsync(session.Filter, cancellationToken);
            var preview = new AdminBroadcastPreviewDto(session.Text, session.PhotoFileId, null, null, session.CalculatedReach, session.Filter);
            await adminPromptService.SendBroadcastPreviewAsync(chatId, preview, cancellationToken);
            return true;
        }

        if (data == "adm_bcast:cancel")
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            adminBroadcastService.ClearSession(callbackQuery.From.Id);
            user.State = UserState.Admin_Panel;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Broadcast_Cancelled"),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            await adminPromptService.SendAdminPanelAsync(chatId, null, cancellationToken);
            return true;
        }

        if (data == "adm_bcast:send")
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            var session = adminBroadcastService.GetOrCreateSession(callbackQuery.From.Id);

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Broadcast_Progress", session.CalculatedReach),
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );

            var result = await adminBroadcastService.ExecuteBroadcastAsync(session, cancellationToken);
            adminBroadcastService.ClearSession(callbackQuery.From.Id);

            await botClient.SendMessage(
                chatId: chatId,
                text: loc.Get(lang, "Admin_Broadcast_Completed", result.TotalTargets, result.DeliveredCount, result.FailedCount, result.Elapsed.TotalSeconds),
                parseMode: ParseMode.Html,
                replyMarkup: AdminKeyboards.GetAdminPanelKeyboard(await adminService.GetPendingReportsCountAsync(cancellationToken), lang),
                cancellationToken: cancellationToken
            );
            return true;
        }

        // 4. Поиск анкет администратором: выбор пола (adm_search_gen:*)
        if (data.StartsWith("adm_search_gen:"))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            var genderStr = data["adm_search_gen:".Length..];
            var gender = genderStr == "male" ? Gender.Male : Gender.Female;

            user.State = UserState.Admin_BrowsingProfiles;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var (profile, totalCount, curIdx) = await adminService.GetAdminProfileByGenderAsync(gender, 0, cancellationToken);
            if (profile is null || totalCount == 0)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "Admin_Search_Empty"),
                    parseMode: ParseMode.Html,
                    replyMarkup: AdminKeyboards.GetAdminSearchGenderKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                return true;
            }

            await adminPromptService.SendAdminCandidateCardAsync(chatId, profile, gender, curIdx, totalCount, 1, cancellationToken);
            return true;
        }

        // 5. Действия в админ-поиске: adm_s_next / adm_s_ban / adm_s_del
        if (data.StartsWith("adm_s_next:"))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            var parts = data.Split(':');
            var gender = parts[1] == "male" ? Gender.Male : Gender.Female;
            var offset = int.TryParse(parts[2], out var o) ? o : 0;

            var (profile, totalCount, curIdx) = await adminService.GetAdminProfileByGenderAsync(gender, offset, cancellationToken);
            if (profile is null || totalCount == 0)
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "Admin_Search_Empty"),
                    parseMode: ParseMode.Html,
                    replyMarkup: AdminKeyboards.GetAdminSearchGenderKeyboard(lang),
                    cancellationToken: cancellationToken
                );
                return true;
            }

            await adminPromptService.SendAdminCandidateCardAsync(chatId, profile, gender, curIdx, totalCount, offset + 1, cancellationToken);
            return true;
        }

        if (data.StartsWith("adm_s_ban:"))
        {
            var parts = data.Split(':');
            var targetUserId = Guid.TryParse(parts[1], out var uid) ? uid : Guid.Empty;
            var gender = parts[2] == "male" ? Gender.Male : Gender.Female;
            var offset = int.TryParse(parts[3], out var o) ? o : 0;

            var result = await adminService.BanUserDirectlyAsync(targetUserId, cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    loc.Get(lang, "Admin_Search_Blocked_Alert"),
                    showAlert: true,
                    cancellationToken: cancellationToken
                );

                // Уведомление нарушителю на его родном языке
                await NotifyViolatorBannedSafeAsync(result.Value.TelegramId, result.Value.Language, cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка блокировки", cancellationToken: cancellationToken);
            }

            var (profile, totalCount, curIdx) = await adminService.GetAdminProfileByGenderAsync(gender, offset, cancellationToken);
            if (profile is not null && totalCount > 0)
            {
                await adminPromptService.SendAdminCandidateCardAsync(chatId, profile, gender, curIdx, totalCount, offset + 1, cancellationToken);
            }
            else
            {
                await botClient.SendMessage(chatId, loc.Get(lang, "Admin_Search_Empty"), cancellationToken: cancellationToken);
            }
            return true;
        }

        if (data.StartsWith("adm_s_del:"))
        {
            var parts = data.Split(':');
            var targetUserId = Guid.TryParse(parts[1], out var uid) ? uid : Guid.Empty;
            var gender = parts[2] == "male" ? Gender.Male : Gender.Female;
            var offset = int.TryParse(parts[3], out var o) ? o : 0;

            var result = await adminService.DeleteUserProfileDirectlyAsync(targetUserId, cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    loc.Get(lang, "Admin_Search_Deleted_Alert"),
                    showAlert: true,
                    cancellationToken: cancellationToken
                );

                // Уведомление нарушителю на его родном языке
                await NotifyViolatorProfileDeletedSafeAsync(result.Value.TelegramId, result.Value.Language, cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQuery(callbackQuery.Id, "Ошибка удаления анкеты", cancellationToken: cancellationToken);
            }

            var (profile, totalCount, curIdx) = await adminService.GetAdminProfileByGenderAsync(gender, offset, cancellationToken);
            if (profile is not null && totalCount > 0)
            {
                await adminPromptService.SendAdminCandidateCardAsync(chatId, profile, gender, curIdx, totalCount, offset + 1, cancellationToken);
            }
            else
            {
                await botClient.SendMessage(chatId, loc.Get(lang, "Admin_Search_Empty"), cancellationToken: cancellationToken);
            }
            return true;
        }

        // 6. Действия по жалобам из админ-панели: adm_rep_ban / adm_rep_del / adm_rep_ign / adm_rep_next
        if (data.StartsWith("adm_rep_ban:") || data.StartsWith("adm_rep_del:") || data.StartsWith("adm_rep_ign:"))
        {
            var parts = data.Split(':');
            var reportId = Guid.TryParse(parts[1], out var rId) ? rId : Guid.Empty;
            var nextSkip = int.TryParse(parts[2], out var s) ? s : 0;

            if (data.StartsWith("adm_rep_ban:"))
            {
                var result = await moderationService.BanUserByReportAsync(reportId, cancellationToken);
                if (result.IsSuccess && result.Value is not null)
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, loc.Get(lang, "Admin_Decision_UserBanned"), showAlert: true, cancellationToken: cancellationToken);
                    await NotifyReporterSafeAsync(result.Value.ReporterTelegramId, result.Value.ReporterLanguage, cancellationToken);
                    await NotifyViolatorBannedSafeAsync(result.Value.ReportedTelegramId, result.Value.ReportedLanguage, cancellationToken);
                }
            }
            else if (data.StartsWith("adm_rep_del:"))
            {
                var result = await moderationService.DeleteProfileByReportAsync(reportId, cancellationToken);
                if (result.IsSuccess && result.Value is not null)
                {
                    await botClient.AnswerCallbackQuery(callbackQuery.Id, loc.Get(lang, "Admin_Decision_ProfileDeleted"), showAlert: true, cancellationToken: cancellationToken);
                    await NotifyReporterSafeAsync(result.Value.ReporterTelegramId, result.Value.ReporterLanguage, cancellationToken);
                    await NotifyViolatorProfileDeletedSafeAsync(result.Value.ReportedTelegramId, result.Value.ReportedLanguage, cancellationToken);
                }
            }
            else if (data.StartsWith("adm_rep_ign:"))
            {
                await moderationService.IgnoreReportAsync(reportId, cancellationToken);
                await botClient.AnswerCallbackQuery(callbackQuery.Id, loc.Get(lang, "Admin_Decision_ReportIgnored"), cancellationToken: cancellationToken);
            }

            var nextReports = await adminService.GetPendingReportsAsync(0, 1, cancellationToken);
            var remainingCount = await adminService.GetPendingReportsCountAsync(cancellationToken);
            if (nextReports.Count > 0)
            {
                await adminPromptService.SendPendingReportCardAsync(chatId, nextReports[0], 1, remainingCount, 1, cancellationToken);
            }
            else
            {
                await botClient.SendMessage(
                    chatId: chatId,
                    text: loc.Get(lang, "Admin_Reports_NoPending"),
                    replyMarkup: AdminKeyboards.GetAdminPanelKeyboard(0, lang),
                    cancellationToken: cancellationToken
                );
            }
            return true;
        }

        if (data.StartsWith("adm_rep_next:"))
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            var parts = data.Split(':');
            var skip = int.TryParse(parts[1], out var s) ? s : 0;

            var totalCount = await adminService.GetPendingReportsCountAsync(cancellationToken);
            if (totalCount == 0)
            {
                await botClient.SendMessage(chatId, loc.Get(lang, "Admin_Reports_NoPending"), replyMarkup: AdminKeyboards.GetAdminPanelKeyboard(0, lang), cancellationToken: cancellationToken);
                return true;
            }

            var normalizedSkip = skip % totalCount;
            var reports = await adminService.GetPendingReportsAsync(normalizedSkip, 1, cancellationToken);
            if (reports.Count > 0)
            {
                await adminPromptService.SendPendingReportCardAsync(chatId, reports[0], normalizedSkip + 1, totalCount, normalizedSkip + 1, cancellationToken);
            }
            return true;
        }

        return false;
    }

    private async Task NotifyReporterSafeAsync(long reporterTelegramId, AppLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var message = loc.Get(language, "Notification_ReportResolved");
            await botClient.SendMessage(
                chatId: reporterTelegramId,
                text: message,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning("Не удалось отправить уведомление заявителю {ReporterTelegramId}: {ErrorMessage}", reporterTelegramId, ex.Message);
        }
    }

    private async Task NotifyViolatorBannedSafeAsync(long violatorTelegramId, AppLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var message = loc.Get(language, "Notification_ViolatorBanned");
            await botClient.SendMessage(
                chatId: violatorTelegramId,
                text: message,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning("Не удалось отправить уведомление о бане нарушителю {ViolatorTelegramId}: {ErrorMessage}", violatorTelegramId, ex.Message);
        }
    }

    private async Task NotifyViolatorProfileDeletedSafeAsync(long violatorTelegramId, AppLanguage language, CancellationToken cancellationToken)
    {
        try
        {
            var message = loc.Get(language, "Notification_ViolatorProfileDeleted");
            await botClient.SendMessage(
                chatId: violatorTelegramId,
                text: message,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning("Не удалось отправить уведомление об удалении профиля нарушителю {ViolatorTelegramId}: {ErrorMessage}", violatorTelegramId, ex.Message);
        }
    }
}
