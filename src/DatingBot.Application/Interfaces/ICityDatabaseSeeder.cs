namespace DatingBot.Application.Interfaces;

public interface ICityDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
