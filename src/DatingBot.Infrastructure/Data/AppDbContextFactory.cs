using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DatingBot.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=DatingBotDb;Trusted_Connection=True;TrustServerCertificate=True;");

        return new AppDbContext(optionsBuilder.Options);
    }
}
