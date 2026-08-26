using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Keyboards;

public static class MainMenuKeyboards
{
    private static readonly ILocalizationService Loc = new LocalizationService();

    public static ReplyKeyboardMarkup GetMainMenuReplyKeyboard(AppLanguage language = AppLanguage.Russian)
    {
        return new ReplyKeyboardMarkup([
            [
                new KeyboardButton(Loc.Get(language, "Menu_Search")),
                new KeyboardButton(Loc.Get(language, "Menu_Profile"))
            ],
            [
                new KeyboardButton(Loc.Get(language, "Menu_Referral")),
                new KeyboardButton(Loc.Get(language, "Menu_Guide"))
            ]
        ])
        {
            ResizeKeyboard = true
        };
    }
}
