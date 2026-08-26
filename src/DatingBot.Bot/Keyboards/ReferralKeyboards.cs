using DatingBot.Application.Interfaces;
using DatingBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Keyboards;

public static class ReferralKeyboards
{
    public static InlineKeyboardMarkup GetReferralMenuInlineKeyboard(AppLanguage language, ILocalizationService loc, bool isAdmin = false)
    {
        var rows = new List<InlineKeyboardButton[]>
        {
            new[] { InlineKeyboardButton.WithCallbackData(loc.Get(language, "Btn_MyReferralLinks"), "ref_my_links") },
            new[] { InlineKeyboardButton.WithCallbackData(loc.Get(language, "Btn_CreateReferralLink"), "ref_create_link") }
        };

        if (isAdmin)
        {
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(loc.Get(language, "Btn_ReferralReport"), "ref_admin_report") });
        }

        return new InlineKeyboardMarkup(rows);
    }
}
