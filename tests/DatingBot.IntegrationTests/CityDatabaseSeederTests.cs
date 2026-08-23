using DatingBot.Infrastructure.Data;
using DatingBot.Infrastructure.Data.Seeds;
using DatingBot.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DatingBot.IntegrationTests;

public class CityDatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_ShouldSeedAllCitiesFromEmbeddedJson()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var seeder = new CityDatabaseSeeder(context, NullLogger<CityDatabaseSeeder>.Instance);

        // Act
        await seeder.SeedAsync();

        // Assert
        var totalCount = await context.Cities.CountAsync();
        totalCount.Should().BeGreaterThan(100000);

        // Check key cities exist
        var moscow = await context.Cities.FirstOrDefaultAsync(c => c.Name == "Москва");
        var kyiv = await context.Cities.FirstOrDefaultAsync(c => c.Name == "Киев");
        var minsk = await context.Cities.FirstOrDefaultAsync(c => c.Name == "Минск");
        var astana = await context.Cities.FirstOrDefaultAsync(c => c.Name == "Астана");
        var berlin = await context.Cities.FirstOrDefaultAsync(c => c.Name == "Берлин");
        var paris = await context.Cities.FirstOrDefaultAsync(c => c.Name == "Париж");

        moscow.Should().NotBeNull();
        kyiv.Should().NotBeNull();
        minsk.Should().NotBeNull();
        astana.Should().NotBeNull();
        berlin.Should().NotBeNull();
        paris.Should().NotBeNull();

        // Check Indonesian and international cities
        var jakartaLatin = await context.Cities.FirstOrDefaultAsync(c => c.Name == "Jakarta");
        var jakartaCyr = await context.Cities.FirstOrDefaultAsync(c => c.Name == "Джакарта");
        var surabaya = await context.Cities.FirstOrDefaultAsync(c => c.Name == "Surabaya");
        var denpasar = await context.Cities.FirstOrDefaultAsync(c => c.Name == "Denpasar");

        jakartaLatin.Should().NotBeNull();
        jakartaCyr.Should().NotBeNull();
        surabaya.Should().NotBeNull();
        denpasar.Should().NotBeNull();
    }
}
