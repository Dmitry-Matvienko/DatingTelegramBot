using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Keyboards;

public static class PaymentKeyboards
{
    private static readonly ILocalizationService Loc = new LocalizationService();

    public static InlineKeyboardMarkup GetUnbanKeyboard(AppLanguage language = AppLanguage.Russian, ILocalizationService? loc = null, int priceStars = 100)
    {
        var localizer = loc ?? Loc;
        var template = localizer.Get(language, "Btn_PayUnbanStars");
        var buttonText = string.Format(template, priceStars);
        return new InlineKeyboardMarkup([
            [
                InlineKeyboardButton.WithCallbackData(buttonText, "pay_unban")
            ]
        ]);
    }
}
