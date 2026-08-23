using DatingBot.Application;
using DatingBot.Bot;
using DatingBot.Bot.Handlers;
using DatingBot.Bot.Services;
using DatingBot.Bot.Workers;
using DatingBot.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

// Предотвращение сбоев FileSystemWatcher (inotify / SIGSEGV 139) в Linux Docker контейнерах
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");

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

// 1. Слой инфраструктуры (EF Core & MS SQL)
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Слой сценариев и бизнес-логики (Application)
builder.Services.AddApplicationServices();

// 3. Telegram Bot Client
builder.Services.AddSingleton<ITelegramBotClient>(_ => BotSetup.CreateBotClient(builder.Configuration));

// 4. Презентационный слой бота
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
builder.Services.AddScoped<TelegramUpdateRouter>();

// 5. Фоновые сервисы
builder.Services.AddHostedService<TelegramBotWorker>();
builder.Services.AddHostedService<MatchmakingNotificationWorker>();
builder.Services.AddHostedService<InactivityNotificationWorker>();

var app = builder.Build();

// Keep-Alive и Health-Check эндпоинты для Render / cron-job.org / Uptime-мониторинга
app.MapGet("/", () => Results.Ok(new
{
    service = "DatingBot",
    status = "running",
    serverTimeUtc = DateTimeOffset.UtcNow
}));

app.MapGet("/ping", () => Results.Text("pong"));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", serverTimeUtc = DateTimeOffset.UtcNow }));
app.MapGet("/healthz", () => Results.Ok(new { status = "Healthy" }));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DatingBot.Infrastructure.Data.AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("DatabaseBootstrap");

    var connString = DatingBot.Infrastructure.DependencyInjection.ResolveConnectionString(config);

    try
    {
        var builderInfo = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
        logger.LogInformation("Подключение к БД: Сервер='{Server}', База='{Database}', Пользователь='{User}'",
            builderInfo.DataSource,
            builderInfo.InitialCatalog,
            string.IsNullOrEmpty(builderInfo.UserID) ? "WindowsAuth" : builderInfo.UserID);
    }
    catch
    {
        logger.LogInformation("Строка подключения к БД получена из конфигурации/переменных окружения.");
    }

    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }

    var seeder = scope.ServiceProvider.GetRequiredService<DatingBot.Application.Interfaces.ICityDatabaseSeeder>();
    await seeder.SeedAsync();
}

await app.RunAsync();

public partial class Program { }
