using DatingBot.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace DatingBot.Bot.Services;

public class BotInfoProvider(
    ITelegramBotClient botClient,
    IConfiguration configuration) : IBotInfoProvider
{
    private string? _cachedUsername;

    public async Task<string> GetBotUsernameAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cachedUsername))
            return _cachedUsername;

        var configured = configuration["BotConfiguration:BotUsername"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            _cachedUsername = configured.Trim().TrimStart('@');
            return _cachedUsername;
        }

        try
        {
            var me = await botClient.GetMe(cancellationToken);
            if (!string.IsNullOrWhiteSpace(me.Username))
            {
                _cachedUsername = me.Username;
                return _cachedUsername;
            }
        }
        catch
        {
            // fallback
        }

        return _cachedUsername ?? "DatingBot";
    }
}
