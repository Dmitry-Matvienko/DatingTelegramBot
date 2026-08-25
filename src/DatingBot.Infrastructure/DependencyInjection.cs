using DatingBot.Application.Interfaces;
using DatingBot.Infrastructure.Data;
using DatingBot.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DatingBot.Infrastructure;

public static class DependencyInjection
{
    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration["DEFAULT_CONNECTION"]
            ?? configuration["DATABASE_URL"]
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Trim() == "YOUR_CONNECTION_STRING_HERE")
        {
            if (OperatingSystem.IsWindows())
            {
                return "Server=(localdb)\\mssqllocaldb;Database=DatingBotDb;Trusted_Connection=True;TrustServerCertificate=True;";
            }

            throw new InvalidOperationException(
                "Строка подключения к базе данных не задана для Linux/Render окружения.\n" +
                "Пожалуйста, добавьте переменную окружения 'DEFAULT_CONNECTION' или 'ConnectionStrings__DefaultConnection' " +
                "в панели Render со строкой подключения к вашей MS SQL базе данных (SmarterASP.NET).");
        }

        if (!OperatingSystem.IsWindows() && connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"В переменных окружения Linux/Render указана строка подключения LocalDB ('{connectionString}'), которая не поддерживается на Linux.\n" +
                "Пожалуйста, укажите строку подключения к удаленной базе данных MS SQL (SmarterASP.NET) в переменной окружения 'DEFAULT_CONNECTION' или 'ConnectionStrings__DefaultConnection'.");
        }

        return connectionString;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IInterestRepository, InterestRepository>();
        services.AddScoped<IProfileRatingRepository, ProfileRatingRepository>();
        services.AddScoped<IProfileReportRepository, ProfileReportRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddSingleton<IAiEmbeddingService, Services.LocalAiEmbeddingService>();
        services.AddSingleton<IAdminSettings, Services.AdminSettings>();
        services.AddScoped<ICityDatabaseSeeder, Data.Seeds.CityDatabaseSeeder>();
        services.AddHttpClient<IGeocodingService, Services.NominatimGeocodingService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
