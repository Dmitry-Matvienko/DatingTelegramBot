using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DatingBot.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true);

        var botPath = Path.Combine(basePath, "src", "DatingBot.Bot");
        if (Directory.Exists(botPath))
        {
            configBuilder
                .AddJsonFile(Path.Combine(botPath, "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine(botPath, "appsettings.Local.json"), optional: true);
        }

        configBuilder.AddEnvironmentVariables();
        var config = configBuilder.Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=DatingBotDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}

