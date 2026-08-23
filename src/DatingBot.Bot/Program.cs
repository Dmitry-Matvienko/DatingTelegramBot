using DatingBot.Application;
using DatingBot.Bot.Handlers;
using DatingBot.Bot.Services;
using DatingBot.Bot.Workers;
using DatingBot.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        config.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // 1. Слой инфраструктуры (EF Core & MS SQL)
        services.AddInfrastructureServices(context.Configuration);

        // 2. Слой сценариев и бизнес-логики (Application)
        services.AddApplicationServices();

        // 3. Telegram Bot Client
        var botToken = context.Configuration["BotConfiguration:BotToken"]
            ?? throw new InvalidOperationException("BotConfiguration:BotToken не найден в конфигурации.");
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

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
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DatingBot.Infrastructure.Data.AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DatingBot.Application.Interfaces.ICityDatabaseSeeder>();
    await seeder.SeedAsync();
}

await app.RunAsync();
