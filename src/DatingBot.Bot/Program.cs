using DatingBot.Application;
using DatingBot.Application.Interfaces;
using DatingBot.Bot;
using DatingBot.Bot.Handlers;
using DatingBot.Bot.Services;
using DatingBot.Bot.Workers;
using DatingBot.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

// Предотвращение сбоев FileSystemWatcher (inotify / SIGSEGV 139) в Linux Docker контейнерах
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");

// Глобальные обработчики необработанных исключений для предотвращения аварийного завершения процесса
AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
{
    var ex = eventArgs.ExceptionObject as Exception;
    Console.Error.WriteLine($"[CRITICAL] Необработанное исключение AppDomain: {ex?.Message}\n{ex?.StackTrace}");
};

TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
{
    Console.Error.WriteLine($"[WARNING] Ненаблюдаемое исключение TaskScheduler: {eventArgs.Exception.Message}");
    eventArgs.SetObserved();
};

var builder = WebApplication.CreateBuilder(args);

// Настройка источников конфигурации
builder.Configuration.SetBasePath(AppContext.BaseDirectory);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// Резервный поиск appsettings.Local.json в текущем каталоге и подпапке src/DatingBot.Bot (при запуске через dotnet run)
if (!string.Equals(Directory.GetCurrentDirectory(), AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase))
{
    var localInCurrentDir = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");
    if (File.Exists(localInCurrentDir))
    {
        builder.Configuration.AddJsonFile(localInCurrentDir, optional: true, reloadOnChange: false);
    }

    var localInBotDir = Path.Combine(Directory.GetCurrentDirectory(), "src", "DatingBot.Bot", "appsettings.Local.json");
    if (File.Exists(localInBotDir))
    {
        builder.Configuration.AddJsonFile(localInBotDir, optional: true, reloadOnChange: false);
    }
}

builder.Configuration.AddEnvironmentVariables();

// Настройка порта для Render / облачных сервисов через переменную PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Защита от остановки хоста при сбоях в фоновых сервисах (Self-Healing Background Services)
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

// Координатор жизненного цикла и отказоустойчивости
builder.Services.AddSingleton<IBotLifecycleCoordinator, BotLifecycleCoordinator>();

// 1. Слой инфраструктуры (EF Core & MS SQL)
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Слой сценариев и бизнес-логики (Application)
builder.Services.AddApplicationServices();

// 3. Telegram Bot Client
builder.Services.AddSingleton<ITelegramBotClient>(_ => BotSetup.CreateBotClient(builder.Configuration));

// 4. Презентационный слой бота
builder.Services.AddSingleton<IBotInfoProvider, BotInfoProvider>();
builder.Services.AddScoped<RegistrationPromptService>();
builder.Services.AddScoped<RegistrationMessageHandler>();
builder.Services.AddScoped<RegistrationCallbackHandler>();
builder.Services.AddScoped<ProfilePromptService>();
builder.Services.AddScoped<ProfileEditMessageHandler>();
builder.Services.AddScoped<ProfileEditCallbackHandler>();
builder.Services.AddScoped<SearchPromptService>();
builder.Services.AddScoped<SearchCallbackHandler>();
builder.Services.AddScoped<AdminModerationCallbackHandler>();
builder.Services.AddScoped<AdminPromptService>();
builder.Services.AddSingleton<AdminBroadcastService>();
builder.Services.AddScoped<AdminCallbackHandler>();
builder.Services.AddScoped<AdminMessageHandler>();
builder.Services.AddScoped<ReferralPromptService>();
builder.Services.AddScoped<TelegramUpdateRouter>();

// 5. Фоновые сервисы (порядок инициализации: БД -> Telegram Bot -> Уведомления)
builder.Services.AddHostedService<DatabaseBootstrapWorker>();
builder.Services.AddHostedService<TelegramBotWorker>();
builder.Services.AddHostedService<MatchmakingNotificationWorker>();
builder.Services.AddHostedService<InactivityNotificationWorker>();

var app = builder.Build();

// Keep-Alive, Health-Check и телеметрия для Render / cron-job.org / Uptime-мониторинга
app.MapGet("/", (IBotLifecycleCoordinator lifecycle) => Results.Ok(new
{
    service = "DatingBot",
    status = lifecycle.IsDatabaseReady ? "running" : "bootstrapping",
    isDatabaseReady = lifecycle.IsDatabaseReady,
    isTelegramPollingActive = lifecycle.IsTelegramPollingActive,
    databaseRetryCount = lifecycle.DatabaseRetryCount,
    telegramRestartCount = lifecycle.TelegramRestartCount,
    lastDatabaseError = lifecycle.LastDatabaseError,
    lastTelegramError = lifecycle.LastTelegramError,
    uptime = (DateTimeOffset.UtcNow - lifecycle.StartedAtUtc).ToString(@"d\.hh\:mm\:ss"),
    serverTimeUtc = DateTimeOffset.UtcNow
}));

app.MapGet("/ping", () => Results.Text("pong"));

app.MapGet("/health", (IBotLifecycleCoordinator lifecycle) => Results.Ok(new
{
    status = lifecycle.IsDatabaseReady ? "Healthy" : "Degraded",
    isDatabaseReady = lifecycle.IsDatabaseReady,
    isTelegramPollingActive = lifecycle.IsTelegramPollingActive,
    serverTimeUtc = DateTimeOffset.UtcNow
}));

app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy" }));

// Логирование параметров подключения к БД перед запуском приложения
try
{
    var connString = DatingBot.Infrastructure.DependencyInjection.ResolveConnectionString(app.Configuration);
    var builderInfo = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Параметры БД: Сервер='{Server}', База='{Database}', Пользователь='{User}'",
        builderInfo.DataSource,
        builderInfo.InitialCatalog,
        string.IsNullOrEmpty(builderInfo.UserID) ? "WindowsAuth" : builderInfo.UserID);
}
catch
{
    // Безопасный перехват при нестандартной строке подключения
}

await app.RunAsync();

public partial class Program { }
