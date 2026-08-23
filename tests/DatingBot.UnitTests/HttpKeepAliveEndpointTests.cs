using System.Net;
using DatingBot.Application.Interfaces;
using DatingBot.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Telegram.Bot;
using Xunit;

namespace DatingBot.UnitTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["BotConfiguration:BotToken"] = "123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ_1234567",
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=DatingBotTestDb;Trusted_Connection=True;"
            };
            config.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Очищаем существующие регистрации EF Core
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            // Регистрируем InMemory
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("KeepAliveTestDb");
            });

            // Мокаем CityDatabaseSeeder
            services.RemoveAll<ICityDatabaseSeeder>();
            var mockSeeder = new Mock<ICityDatabaseSeeder>();
            mockSeeder.Setup(s => s.SeedAsync(default)).Returns(Task.CompletedTask);
            services.AddSingleton(mockSeeder.Object);

            // Заменяем TelegramBotClient на мок
            services.RemoveAll<ITelegramBotClient>();
            var mockBotClient = new Mock<ITelegramBotClient>();
            services.AddSingleton(mockBotClient.Object);

            // Удаляем фоновые сервисы
            var hostedServiceDescriptors = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var hosted in hostedServiceDescriptors)
            {
                services.Remove(hosted);
            }
        });
    }
}

public class HttpKeepAliveEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HttpKeepAliveEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RootEndpoint_ReturnsSuccessAndRunningStatus()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("DatingBot");
        content.Should().Contain("running");
    }

    [Fact]
    public async Task PingEndpoint_ReturnsPong()
    {
        // Act
        var response = await _client.GetAsync("/ping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("pong");
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task HealthzEndpoint_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/healthz");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
