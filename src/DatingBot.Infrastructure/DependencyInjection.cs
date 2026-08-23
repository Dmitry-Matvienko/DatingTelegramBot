using DatingBot.Application.Interfaces;
using DatingBot.Infrastructure.Data;
using DatingBot.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DatingBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DEFAULT_CONNECTION"]
            ?? configuration["DATABASE_URL"]
            ?? "Server=localhost;Database=DatingBotDb;Trusted_Connection=True;TrustServerCertificate=True;";

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
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
