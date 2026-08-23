using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace DatingBot.Bot;

/// <summary>
/// Фабрика и валидатор конфигурации Telegram-бота.
/// </summary>
public static class BotSetup
{
    /// <summary>
    /// Создает экземпляр ITelegramBotClient с валидацией наличия и корректности формата BotToken.
    /// </summary>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <returns>Настроенный ITelegramBotClient.</returns>
    /// <exception cref="ArgumentNullException">Если конфигурация равна null.</exception>
    /// <exception cref="InvalidOperationException">Если токен не задан, содержит плейсхолдер или имеет невалидный формат.</exception>
    public static ITelegramBotClient CreateBotClient(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var botToken = configuration["BotConfiguration:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken) || botToken.Trim() == "YOUR_BOT_TOKEN_HERE")
        {
            throw new InvalidOperationException(
                "BotConfiguration:BotToken не задан или содержит плейсхолдер 'YOUR_BOT_TOKEN_HERE'.\n" +
                "Пожалуйста, укажите валидный токен Telegram-бота в файле appsettings.Local.json или через переменную окружения 'BotConfiguration__BotToken'.\n" +
                "Пример appsettings.Local.json:\n" +
                "{\n" +
                "  \"BotConfiguration\": {\n" +
                "    \"BotToken\": \"123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ_1234567\",\n" +
                "    \"AdminIds\": [123456789]\n" +
                "  }\n" +
                "}");
        }

        try
        {
            return new TelegramBotClient(botToken.Trim());
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"Указанный токен Telegram-бота имеет неверный формат (BotToken: '{botToken}').\n" +
                "Токен от @BotFather должен иметь формат вида '123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ_1234567'.\n" +
                "Проверьте настройки в appsettings.Local.json или переменной окружения 'BotConfiguration__BotToken'.", ex);
        }
    }
}
