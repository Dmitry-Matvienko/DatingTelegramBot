using DatingBot.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DatingBot.UnitTests;

public class AdminSettingsTests
{
    [Fact]
    public void AdminIds_WhenConfiguredAsArray_ReturnsParsedIds()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["BotConfiguration:AdminIds:0"] = "111111",
            ["BotConfiguration:AdminIds:1"] = "222222"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var adminSettings = new AdminSettings(config);

        // Act
        var ids = adminSettings.AdminIds;

        // Assert
        ids.Should().BeEquivalentTo([111111L, 222222L]);
    }

    [Fact]
    public void AdminIds_WhenConfiguredAsCommaSeparatedString_ReturnsParsedIds()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["BotConfiguration:AdminIds"] = "111111, 222222; 333333"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var adminSettings = new AdminSettings(config);

        // Act
        var ids = adminSettings.AdminIds;

        // Assert
        ids.Should().BeEquivalentTo([111111L, 222222L, 333333L]);
    }

    [Fact]
    public void AdminIds_WhenConfiguredViaAdminIdsEnvKey_ReturnsParsedIds()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["ADMIN_IDS"] = "999888, 777666"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var adminSettings = new AdminSettings(config);

        // Act
        var ids = adminSettings.AdminIds;

        // Assert
        ids.Should().BeEquivalentTo([999888L, 777666L]);
    }

    [Fact]
    public void AdminIds_WhenEmptyOrInvalid_ReturnsEmptyList()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["BotConfiguration:AdminIds"] = "invalid, zero, -5"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var adminSettings = new AdminSettings(config);

        // Act
        var ids = adminSettings.AdminIds;

        // Assert
        ids.Should().BeEmpty();
    }
}
