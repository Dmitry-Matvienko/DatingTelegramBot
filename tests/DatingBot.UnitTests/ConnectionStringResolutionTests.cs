using DatingBot.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DatingBot.UnitTests;

public class ConnectionStringResolutionTests
{
    [Fact]
    public void ResolveConnectionString_WhenDefaultConnectionEnvProvided_UsesIt()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["DEFAULT_CONNECTION"] = "Server=remote.db.com;Database=RemoteDb;User Id=usr;Password=pwd;"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        // Act
        var connStr = DependencyInjection.ResolveConnectionString(config);

        // Assert
        connStr.Should().Be("Server=remote.db.com;Database=RemoteDb;User Id=usr;Password=pwd;");
    }

    [Fact]
    public void ResolveConnectionString_WhenDatabaseUrlEnvProvided_UsesIt()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["DATABASE_URL"] = "Server=remote.db.com;Database=RemoteDb2;User Id=usr;Password=pwd;"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        // Act
        var connStr = DependencyInjection.ResolveConnectionString(config);

        // Assert
        connStr.Should().Be("Server=remote.db.com;Database=RemoteDb2;User Id=usr;Password=pwd;");
    }

    [Fact]
    public void ResolveConnectionString_WhenStandardConnectionStringsProvided_UsesIt()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=remote.db.com;Database=RemoteDb3;User Id=usr;Password=pwd;"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        // Act
        var connStr = DependencyInjection.ResolveConnectionString(config);

        // Assert
        connStr.Should().Be("Server=remote.db.com;Database=RemoteDb3;User Id=usr;Password=pwd;");
    }

    [Fact]
    public void AddInfrastructureServices_RegistersDbContextWithResolvedConnectionString()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["DEFAULT_CONNECTION"] = "Server=remote.db.com;Database=RemoteDb4;User Id=usr;Password=pwd;TrustServerCertificate=True;"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructureServices(config);
        var provider = services.BuildServiceProvider();
        var dbContext = provider.GetRequiredService<DatingBot.Infrastructure.Data.AppDbContext>();

        // Assert
        RelationalDatabaseFacadeExtensions.GetConnectionString(dbContext.Database)
            .Should().Be("Server=remote.db.com;Database=RemoteDb4;User Id=usr;Password=pwd;TrustServerCertificate=True;");
    }
}
