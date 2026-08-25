using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Keyboards;

public static class SearchKeyboards
{
    private static readonly ILocalizationService Loc = new LocalizationService();

    public static ReplyKeyboardMarkup GetRatingReplyKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new ReplyKeyboardMarkup([
            [
                new KeyboardButton("1️⃣"),
                new KeyboardButton("2️⃣"),
                new KeyboardButton("3️⃣"),
                new KeyboardButton("4️⃣"),
                new KeyboardButton("5️⃣")
            ],
            [
                new KeyboardButton("6️⃣"),
                new KeyboardButton("7️⃣"),
                new KeyboardButton("8️⃣"),
                new KeyboardButton("9️⃣"),
                new KeyboardButton("🔟")
            ],
            [
                new KeyboardButton(Loc.Get(language, "Btn_Report")),
                new KeyboardButton(Loc.Get(language, "Btn_SearchAgain")),
                new KeyboardButton(Loc.Get(language, "Btn_MainMenu"))
            ]
        ])
        {
            ResizeKeyboard = true
        };
    }

    public static ReplyKeyboardMarkup GetIncomingRatingReplyKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new ReplyKeyboardMarkup([
            [
                new KeyboardButton("1️⃣"),
                new KeyboardButton("2️⃣"),
                new KeyboardButton("3️⃣"),
                new KeyboardButton("4️⃣"),
                new KeyboardButton("5️⃣")
            ],
            [
                new KeyboardButton("6️⃣"),
                new KeyboardButton("7️⃣"),
                new KeyboardButton("8️⃣"),
                new KeyboardButton("9️⃣"),
                new KeyboardButton("🔟")
            ],
            [
                new KeyboardButton(Loc.Get(language, "Btn_Report")),
                new KeyboardButton(Loc.Get(language, "Btn_MainMenu"))
            ]
        ])
        {
            ResizeKeyboard = true
        };
    }

    public static ReplyKeyboardMarkup GetCancelReportReplyKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new ReplyKeyboardMarkup([
            [
                new KeyboardButton(Loc.Get(language, "Btn_Cancel"))
            ]
        ])
        {
            ResizeKeyboard = true
        };
    }

    public static InlineKeyboardMarkup GetReportReasonsKeyboard(Guid candidateProfileId, AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "ReportReason_18Plus"), $"report_reason:{candidateProfileId}:1")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "ReportReason_Inappropriate"), $"report_reason:{candidateProfileId}:2")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "ReportReason_Other"), $"report_reason:{candidateProfileId}:3")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), $"report_cancel:{candidateProfileId}")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetIncomingRatingNotificationKeyboard(Guid ratingId, AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_ShowWhoRated"), $"view_rater:{ratingId}")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetMutualMatchKeyboard(long telegramId, string? username, AppLanguage language = AppLanguage.Russian)
    {
        var userUrl = TelegramUrlHelper.GetUserProfileUrl(telegramId, username);
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithUrl(Loc.Get(language, "Btn_SendMessage"), userUrl)
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetRaterCardKeyboard(long telegramId, string? username, AppLanguage language = AppLanguage.Russian)
    {
        var userUrl = TelegramUrlHelper.GetUserProfileUrl(telegramId, username);
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithUrl(Loc.Get(language, "Btn_SendMessage"), userUrl)
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetNoCandidatesKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_SearchAgain"), "search:reset_city")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_MainMenu"), "search:main_menu")
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetCitySuggestionsKeyboard(IReadOnlyList<City> suggestions, bool isEditing = false, AppLanguage language = AppLanguage.Russian)
    {
        var prefix = isEditing ? "edit_city_confirm" : "reg_city_confirm";
        var cancelPrefix = isEditing ? "edit_city_retry" : "reg_city_retry";

        var rows = new List<InlineKeyboardButton[]>();
        foreach (var city in suggestions)
        {
            var displayText = !string.IsNullOrWhiteSpace(city.Region) && !city.Name.Contains(city.Region, StringComparison.OrdinalIgnoreCase)
                ? $"✅ {city.Name} ({city.Region})"
                : $"✅ {city.Name}";

            rows.Add([InlineKeyboardButton.WithCallbackData(displayText, $"{prefix}:{city.Id}")]);
        }

        rows.Add([InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), cancelPrefix)]);

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup GetCitySuggestionKeyboard(int cityId, string cityName, bool isEditing = false, AppLanguage language = AppLanguage.Russian)
    {
        var prefix = isEditing ? "edit_city_confirm" : "reg_city_confirm";
        var cancelPrefix = isEditing ? "edit_city_retry" : "reg_city_retry";

        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData($"✅ {cityName}", $"{prefix}:{cityId}")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Btn_Cancel"), cancelPrefix)
            ]
        ]);
    }

    public static InlineKeyboardMarkup GetAdminModerationKeyboard(Guid reportId, AppLanguage language = AppLanguage.Russian)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_BanUser"), $"adm_ban:{reportId}")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_DeleteProfile"), $"adm_del:{reportId}")
            ],
            [
                InlineKeyboardButton.WithCallbackData(Loc.Get(language, "Admin_Btn_Ignore"), $"adm_ign:{reportId}")
            ]
        ]);
    }
}
