using DatingBot.Bot;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Xunit;

namespace DatingBot.UnitTests;

public class BotSetupTests
{
    [Fact]
    public void CreateBotClient_WhenConfigurationIsNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => BotSetup.CreateBotClient(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateBotClient_WhenTokenIsNullOrWhitespace_ThrowsInvalidOperationException(string? token)
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>();
        if (token != null)
        {
            inMemorySettings["BotConfiguration:BotToken"] = token;
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        // Act
        var act = () => BotSetup.CreateBotClient(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BotConfiguration:BotToken не задан или содержит плейсхолдер*");
    }

    [Fact]
    public void CreateBotClient_WhenTokenIsPlaceholder_ThrowsInvalidOperationException()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["BotConfiguration:BotToken"] = "YOUR_BOT_TOKEN_HERE"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        // Act
        var act = () => BotSetup.CreateBotClient(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BotConfiguration:BotToken не задан или содержит плейсхолдер 'YOUR_BOT_TOKEN_HERE'*");
    }

    [Theory]
    [InlineData("invalid_token_without_colon")]
    [InlineData("abc:def")]
    [InlineData("12345")]
    public void CreateBotClient_WhenTokenIsMalformed_ThrowsInvalidOperationExceptionWithFormatGuidance(string invalidToken)
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["BotConfiguration:BotToken"] = invalidToken
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        // Act
        var act = () => BotSetup.CreateBotClient(config);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Указанный токен Telegram-бота имеет неверный формат*");
    }

    [Fact]
    public void CreateBotClient_WhenTokenIsValidFormat_ReturnsTelegramBotClient()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["BotConfiguration:BotToken"] = "123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ_1234567"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        // Act
        var client = BotSetup.CreateBotClient(config);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<ITelegramBotClient>();
    }

    [Fact]
    public void CreateBotClient_WhenTokenInBotTokenEnvKey_ReturnsTelegramBotClient()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["BOT_TOKEN"] = "123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ_1234567"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        // Act
        var client = BotSetup.CreateBotClient(config);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<ITelegramBotClient>();
    }
}
