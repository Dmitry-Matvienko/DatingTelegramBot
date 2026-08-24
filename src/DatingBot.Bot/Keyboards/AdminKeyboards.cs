using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Keyboards;

public static class AdminKeyboards
{
    private static readonly ILocalizationService Loc = new LocalizationService();

    public static InlineKeyboardMarkup GetAdminPanelKeyboard(int pendingReportsCount, AppLanguage language = AppLanguage.Russian)
    {
        var reportsText = pendingReportsCount > 0
            ? Loc.Get(language, "Admin_Btn_Reports", pendingReportsCount)
            : Loc.Get(language, "Admin_Btn_NoReports");

        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_Stats"), "adm_panel:stats"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_Broadcast"), "adm_panel:bcast")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_Revenue"), "adm_panel:revenue"),
                InlineKeyboardButton.WithCallbackData(reportsText, "adm_panel:reports")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Menu_Language"), "adm_panel:lang")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminRevenueKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Revenue_Btn_Balance"), "adm_rev:balance"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Revenue_Btn_History"), "adm_rev:history")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_BackToPanel"), "adm_panel:main")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminRevenueDetailsKeyboard(AppLanguage language = AppLanguage.Russian, bool isBalanceScreen = true)
    {
        var switchButton = isBalanceScreen
            ? InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Revenue_Btn_History"), "adm_rev:history")
            : InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Revenue_Btn_Balance"), "adm_rev:balance");

        return new InlineKeyboardMarkup([
            [
                switchButton
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_Revenue"), "adm_panel:revenue"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_BackToPanel"), "adm_panel:main")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminStatsKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Stats_Btn_CitySearch"), "adm_stats:city_search")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_BackToPanel"), "adm_panel:main")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetBackToStatsKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_Stats"), "adm_panel:stats"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_BackToPanel"), "adm_panel:main")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminBroadcastAudienceKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Broadcast_All"), "adm_bcast:all")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Broadcast_Targeted"), "adm_bcast:target")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_BackToPanel"), "adm_panel:main")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminBroadcastGenderKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Broadcast_Gender_All"), "adm_bgender:all")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Broadcast_Gender_Male"), "adm_bgender:male"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Broadcast_Gender_Female"), "adm_bgender:female")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Broadcast_Audience_Friends"), "adm_bgender:friends")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Broadcast_Audience_Relationship"), "adm_bgender:relationship")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Broadcast_Audience_Adult"), "adm_bgender:adult")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "adm_bcast:cancel")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminBroadcastSkipCityKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Skip"), "adm_bcity:skip")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "adm_bcast:cancel")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminBroadcastSkipButtonKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Skip"), "adm_bbtn:skip")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "adm_bcast:cancel")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminBroadcastConfirmKeyboard(string? buttonText, string? buttonUrl, AppLanguage language = AppLanguage.Russian)
    {
        var rows = new List<List<InlineKeyboardButton>>();

        if (!string.IsNullOrWhiteSpace(buttonText) && !string.IsNullOrWhiteSpace(buttonUrl))
        {
            rows.Add([InlineKeyboardButton.WithUrl(buttonText, buttonUrl)]);
        }

        rows.Add([
            InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Broadcast_Btn_Send"), "adm_bcast:send"),
            InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), "adm_bcast:cancel")
        ]);

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup GetAdminSearchGenderKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Gender_Male"), "adm_search_gen:male"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Gender_Female"), "adm_search_gen:female")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminProfileCardKeyboard(Guid userId, long telegramId, string? username, Gender gender, int nextOffset, AppLanguage language = AppLanguage.Russian)
    {
        var genderStr = gender == Gender.Male ? "male" : "female";
        var userUrl = TelegramUrlHelper.GetUserProfileUrl(telegramId, username);

        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithUrl(Loc.Get(language, "Btn_SendMessage"), userUrl)
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Search_Btn_Block"), $"adm_s_ban:{userId}:{genderStr}:{nextOffset}"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Search_Btn_Delete"), $"adm_s_del:{userId}:{genderStr}:{nextOffset}")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Search_Btn_Next"), $"adm_s_next:{genderStr}:{nextOffset}")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminPendingReportKeyboard(Guid reportId, int nextSkip, int totalCount, AppLanguage language = AppLanguage.Russian)
    {
        var rows = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_BanUser"), $"adm_rep_ban:{reportId}:{nextSkip}"),
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_DeleteProfile"), $"adm_rep_del:{reportId}:{nextSkip}")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_Ignore"), $"adm_rep_ign:{reportId}:{nextSkip}")
            }
        };

        if (totalCount > 1)
        {
            rows.Add([InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Reports_Btn_NextReport"), $"adm_rep_next:{nextSkip}")]);
        }

        rows.Add([InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_BackToPanel"), "adm_panel:main")]);

        return new InlineKeyboardMarkup(rows);
    }
}
