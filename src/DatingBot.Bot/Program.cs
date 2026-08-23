using DatingBot.Application;
using DatingBot.Bot;
using DatingBot.Bot.Handlers;
using DatingBot.Bot.Services;
using DatingBot.Bot.Workers;
using DatingBot.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        config.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        // Резервный поиск appsettings.Local.json в текущем каталоге и подпапке src/DatingBot.Bot (при запуске через dotnet run)
        if (!string.Equals(Directory.GetCurrentDirectory(), AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            var localInCurrentDir = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");
            if (File.Exists(localInCurrentDir))
            {
                config.AddJsonFile(localInCurrentDir, optional: true, reloadOnChange: true);
            }

            var localInBotDir = Path.Combine(Directory.GetCurrentDirectory(), "src", "DatingBot.Bot", "appsettings.Local.json");
            if (File.Exists(localInBotDir))
            {
                config.AddJsonFile(localInBotDir, optional: true, reloadOnChange: true);
            }
        }

        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // 1. Слой инфраструктуры (EF Core & MS SQL)
        services.AddInfrastructureServices(context.Configuration);

        // 2. Слой сценариев и бизнес-логики (Application)
        services.AddApplicationServices();

        // 3. Telegram Bot Client
        services.AddSingleton<ITelegramBotClient>(_ => BotSetup.CreateBotClient(context.Configuration));

        // 4. Презентационный слой бота
        services.AddScoped<RegistrationPromptService>();
        services.AddScoped<RegistrationMessageHandler>();
        services.AddScoped<RegistrationCallbackHandler>();
        services.AddScoped<ProfilePromptService>();
        services.AddScoped<ProfileEditMessageHandler>();
        services.AddScoped<ProfileEditCallbackHandler>();
        services.AddScoped<SearchPromptService>();
        services.AddScoped<SearchCallbackHandler>();
        services.AddScoped<AdminModerationCallbackHandler>();
        services.AddScoped<AdminPromptService>();
        services.AddSingleton<AdminBroadcastService>();
        services.AddScoped<AdminCallbackHandler>();
        services.AddScoped<AdminMessageHandler>();
        services.AddScoped<TelegramUpdateRouter>();

        // 5. Фоновые сервисы
        services.AddHostedService<TelegramBotWorker>();
        services.AddHostedService<MatchmakingNotificationWorker>();
        services.AddHostedService<InactivityNotificationWorker>();
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DatingBot.Infrastructure.Data.AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("DatabaseBootstrap");

    var connString = config.GetConnectionString("DefaultConnection") ?? string.Empty;
    var builderInfo = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connString);
    logger.LogInformation("Подключение к БД: Сервер='{Server}', База='{Database}', Пользователь='{User}'",
        builderInfo.DataSource,
        builderInfo.InitialCatalog,
        string.IsNullOrEmpty(builderInfo.UserID) ? "WindowsAuth" : builderInfo.UserID);

    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DatingBot.Application.Interfaces.ICityDatabaseSeeder>();
    await seeder.SeedAsync();
}

await app.RunAsync();
