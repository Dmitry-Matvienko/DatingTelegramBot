namespace DatingBot.Bot.Services;

/// <summary>
/// Координатор жизненного цикла, отказоустойчивости и состояния готовности сервисов бота.
/// </summary>
public interface IBotLifecycleCoordinator
{
    /// <summary>
    /// Флаг готовности базы данных (применены миграции и сидирование).
    /// </summary>
    bool IsDatabaseReady { get; }

    /// <summary>
    /// Флаг активности фонового Long Polling опроса Telegram API.
    /// </summary>
    bool IsTelegramPollingActive { get; }

    /// <summary>
    /// Количество повторных попыток подключения/миграции базы данных.
    /// </summary>
    int DatabaseRetryCount { get; }

    /// <summary>
    /// Количество автоматических самовосстановлений (перезапусков) сессии Telegram API.
    /// </summary>
    int TelegramRestartCount { get; }

    /// <summary>
    /// Сообщение последней ошибки базы данных (если была).
    /// </summary>
    string? LastDatabaseError { get; }

    /// <summary>
    /// Сообщение последней ошибки Telegram API (если была).
    /// </summary>
    string? LastTelegramError { get; }

    /// <summary>
    /// Время запуска процесса (UTC).
    /// </summary>
    DateTimeOffset StartedAtUtc { get; }

    /// <summary>
    /// Время успешной готовности базы данных (UTC).
    /// </summary>
    DateTimeOffset? DatabaseReadyAtUtc { get; }

    /// <summary>
    /// Устанавливает статус успешной готовности базы данных и разблокирует зависимые службы.
    /// </summary>
    void MarkDatabaseReady();

    /// <summary>
    /// Фиксирует ошибку подключения/миграции базы данных.
    /// </summary>
    /// <param name="ex">Возникшее исключение.</param>
    void RecordDatabaseError(Exception ex);

    /// <summary>
    /// Устанавливает статус активности опроса Telegram API.
    /// </summary>
    /// <param name="active">Активен ли опрос.</param>
    void SetTelegramPollingActive(bool active);

    /// <summary>
    /// Фиксирует сбой сессии опроса Telegram API и инкрементирует счетчик перезапусков.
    /// </summary>
    /// <param name="ex">Возникшее исключение.</param>
    void RecordTelegramRestart(Exception ex);

    /// <summary>
    /// Асинхронно ожидает готовности базы данных перед стартом работы с ней.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Task, завершающийся при готовности БД.</returns>
    Task WaitForDatabaseReadyAsync(CancellationToken cancellationToken = default);
}
