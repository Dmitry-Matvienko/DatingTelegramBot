using System.Net;

namespace DatingBot.Bot.Keyboards;

public static class TelegramUrlHelper
{
    /// <summary>
    /// Возвращает валидный публичный HTTPS URL профиля пользователя (https://t.me/username).
    /// Если username отсутствует или пуст, возвращает null.
    /// ВНИМАНИЕ: Ссылки вида "tg://user?id=..." строго запрещены Telegram Bot API для кнопок InlineKeyboardButton.WithUrl
    /// и вызывают ошибку Telegram API "400 Bad Request: BUTTON_URL_INVALID" (или URL_HOST_INVALID).
    /// </summary>
    public static string? GetUserProfileUrl(long telegramId, string? username)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            var cleanUsername = username.Trim().TrimStart('@');
            if (!string.IsNullOrEmpty(cleanUsername))
            {
                return $"https://t.me/{cleanUsername}";
            }
        }

        return null;
    }

    /// <summary>
    /// Формирует безопасную HTML-ссылку на аккаунт пользователя.
    /// Если у пользователя есть никнейм (@username) -> формирует гарантированно кликабельную HTTPS-ссылку https://t.me/username во всех клиентах Telegram.
    /// Если никнейма нет -> формирует deep-link tg://user?id={telegramId}.
    /// </summary>
    public static string FormatUserAccountHtmlLink(long telegramId, string? username, string? displayName)
    {
        var safeName = !string.IsNullOrWhiteSpace(displayName)
            ? WebUtility.HtmlEncode(displayName)
            : "Пользователь";

        if (!string.IsNullOrWhiteSpace(username))
        {
            var cleanUsername = username.Trim().TrimStart('@');
            if (!string.IsNullOrEmpty(cleanUsername))
            {
                var safeUsername = WebUtility.HtmlEncode(cleanUsername);
                return $"<a href=\"https://t.me/{safeUsername}\">{safeName}</a> (@{safeUsername})";
            }
        }

        return $"<a href=\"tg://user?id={telegramId}\">{safeName}</a>";
    }
}
