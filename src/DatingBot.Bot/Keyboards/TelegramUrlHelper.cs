namespace DatingBot.Bot.Keyboards;

public static class TelegramUrlHelper
{
    public static string GetUserProfileUrl(long telegramId, string? username)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            var cleanUsername = username.Trim().TrimStart('@');
            return $"https://t.me/{cleanUsername}";
        }

        return $"tg://user?id={telegramId}";
    }
}
