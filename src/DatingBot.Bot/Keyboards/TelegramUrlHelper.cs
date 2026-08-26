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
}
