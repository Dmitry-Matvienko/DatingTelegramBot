using DatingBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DatingBot.Bot.Keyboards;

public static class LanguageKeyboards
{
    public static InlineKeyboardMarkup GetLanguageSelectionKeyboard(string prefix = "lang")
    {
        return new InlineKeyboardMarkup(
        [
            [
                InlineKeyboardButton.WithCallbackData("🇷🇺 Русский", $"{prefix}_{(int)AppLanguage.Russian}"),
                InlineKeyboardButton.WithCallbackData("🇺🇦 Українська", $"{prefix}_{(int)AppLanguage.Ukrainian}")
            ],
            [
                InlineKeyboardButton.WithCallbackData("🇬🇧 English", $"{prefix}_{(int)AppLanguage.English}"),
                InlineKeyboardButton.WithCallbackData("🇮🇳 हिन्दी", $"{prefix}_{(int)AppLanguage.Hindi}")
            ],
            [
                InlineKeyboardButton.WithCallbackData("🇧🇷 Português", $"{prefix}_{(int)AppLanguage.Portuguese}"),
                InlineKeyboardButton.WithCallbackData("🇮🇩 Bahasa Indonesia", $"{prefix}_{(int)AppLanguage.Indonesian}")
            ]
        ]);
    }
}
