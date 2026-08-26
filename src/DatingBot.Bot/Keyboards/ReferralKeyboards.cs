using DatingBot.Application.Interfaces;
using DatingBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Keyboards;

public static class ReferralKeyboards
{
    public static InlineKeyboardMarkup GetReferralMenuInlineKeyboard(AppLanguage language, ILocalizationService loc)
    {
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(loc.Get(language, "Btn_MyReferralLinks"), "ref_my_links")
            ],
            [
                InlineKeyboardButton.WithCallbackData(loc.Get(language, "Btn_CreateReferralLink"), "ref_create_link")
            ]
        ]);
    }
}
