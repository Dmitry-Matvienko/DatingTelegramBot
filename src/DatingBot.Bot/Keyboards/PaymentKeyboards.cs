using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Keyboards;

public static class PaymentKeyboards
{
    private static readonly ILocalizationService Loc = new LocalizationService();

    public static InlineKeyboardMarkup GetUnbanKeyboard(AppLanguage language = AppLanguage.Russian, ILocalizationService? loc = null)
    {
        var localizer = loc ?? Loc;
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(localizer.Get(language, "Btn_PayUnban100Stars"), "pay_unban")
            ]
        ]);
    }
}
