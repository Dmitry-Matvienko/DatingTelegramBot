namespace DatingBot.Bot.Services;

/// <summary>
/// Потокобезопасная реализация координатора жизненного цикла и самовосстановления бота.
/// </summary>
public class BotLifecycleCoordinator : IBotLifecycleCoordinator
{
    private readonly object _lock = new();
    private readonly TaskCompletionSource<bool> _dbReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _dbRetryCount;
    private int _telegramRestartCount;
    private bool _isTelegramPollingActive;
    private string? _lastDatabaseError;
    private string? _lastTelegramError;
    private DateTimeOffset? _databaseReadyAtUtc;

    public bool IsDatabaseReady => _dbReadyTcs.Task.IsCompletedSuccessfully;

    public bool IsTelegramPollingActive
    {
        get
        {
            lock (_lock)
            {
                return _isTelegramPollingActive;
            }
        }
    }

    public int DatabaseRetryCount => Volatile.Read(ref _dbRetryCount);

    public int TelegramRestartCount => Volatile.Read(ref _telegramRestartCount);

    public string? LastDatabaseError
    {
        get
        {
            lock (_lock)
            {
                return _lastDatabaseError;
            }
        }
    }

    public string? LastTelegramError
    {
        get
        {
            lock (_lock)
            {
                return _lastTelegramError;
            }
        }
    }

    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DatabaseReadyAtUtc
    {
        get
        {
            lock (_lock)
            {
                return _databaseReadyAtUtc;
            }
        }
    }

    public void MarkDatabaseReady()
    {
        lock (_lock)
        {
            _databaseReadyAtUtc = DateTimeOffset.UtcNow;
            _dbReadyTcs.TrySetResult(true);
        }
    }

    public void RecordDatabaseError(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        Interlocked.Increment(ref _dbRetryCount);

        lock (_lock)
        {
            _lastDatabaseError = ex.Message;
        }
    }

    public void SetTelegramPollingActive(bool active)
    {
        lock (_lock)
        {
            _isTelegramPollingActive = active;
        }
    }

    public void RecordTelegramRestart(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        Interlocked.Increment(ref _telegramRestartCount);

        lock (_lock)
        {
            _isTelegramPollingActive = false;
            _lastTelegramError = ex.Message;
        }
    }

    public async Task WaitForDatabaseReadyAsync(CancellationToken cancellationToken = default)
    {
        if (_dbReadyTcs.Task.IsCompletedSuccessfully)
        {
            return;
        }

        await _dbReadyTcs.Task.WaitAsync(cancellationToken);
    }
}
