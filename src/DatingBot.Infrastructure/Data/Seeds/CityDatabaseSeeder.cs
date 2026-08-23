using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DatingBot.Infrastructure.Data.Seeds;

public class CityDatabaseSeeder(
    AppDbContext context,
    ILogger<CityDatabaseSeeder> logger) : ICityDatabaseSeeder
{
    private const int MinimumExpectedCities = 50000;
    private const int BatchSize = 5000;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingCount = await context.Cities.CountAsync(cancellationToken);
        if (existingCount >= MinimumExpectedCities)
        {
            logger.LogInformation("База городов уже содержит {Count} записей. Сидирование не требуется.", existingCount);
            return;
        }

        logger.LogInformation("Начало сидирования полной базы городов и поселков (текущее количество: {Count})...", existingCount);

        var assembly = Assembly.GetExecutingAssembly();
        using var resourceStream = assembly.GetManifestResourceStream("DatingBot.Infrastructure.Data.Datasets.cities_database.json.gz");

        if (resourceStream is null)
        {
            // Fallback к поиску файла на диске
            var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "Datasets", "cities_database.json.gz");
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DatingBot.Infrastructure", "Data", "Datasets", "cities_database.json.gz");
            }

            if (!File.Exists(filePath))
            {
                logger.LogWarning("Файл cities_database.json.gz не найден для сидирования.");
                return;
            }

            using var fileStream = File.OpenRead(filePath);
            await ProcessStreamAndSeedAsync(fileStream, existingCount, cancellationToken);
            return;
        }

        await ProcessStreamAndSeedAsync(resourceStream, existingCount, cancellationToken);
    }

    private async Task ProcessStreamAndSeedAsync(Stream compressedStream, int existingCount, CancellationToken cancellationToken)
    {
        using var gzip = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var memoryStream = new MemoryStream();
        await gzip.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var items = await JsonSerializer.DeserializeAsync<List<CitySeedItem>>(memoryStream, cancellationToken: cancellationToken);
        if (items is null || items.Count == 0)
        {
            logger.LogWarning("Десериализация базы городов вернула 0 записей.");
            return;
        }

        logger.LogInformation("Распаковано {Count} населенных пунктов. Начинается пакетная вставка...", items.Count);

        // Если в базе уже были старые тестовые записи, очистим таблицу перед полной загрузкой
        if (existingCount > 0)
        {
            // Сбрасываем CityId в профилях перед очисткой городов, чтобы не нарушать внешние ключи
            await context.Database.ExecuteSqlRawAsync("UPDATE UserProfiles SET CityId = NULL", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Cities", cancellationToken);
            await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Cities', RESEED, 0)", cancellationToken);
        }

        var prevAutoDetect = context.ChangeTracker.AutoDetectChangesEnabled;
        context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var batch = new List<City>(BatchSize);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                batch.Add(new City
                {
                    Name = item.Name,
                    Region = item.Region,
                    Country = item.Country,
                    Latitude = item.Latitude,
                    Longitude = item.Longitude
                });

                if (batch.Count >= BatchSize || i == items.Count - 1)
                {
                    await context.Cities.AddRangeAsync(batch, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                    context.ChangeTracker.Clear();
                    batch.Clear();
                    logger.LogInformation("Сохранено {Current}/{Total} населенных пунктов...", i + 1, items.Count);
                }
            }

            logger.LogInformation("Сидирование базы городов успешно завершено! Всего в базе: {Count} записей.", items.Count);
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = prevAutoDetect;
        }
    }

    private record CitySeedItem(string Name, string? Region, string Country, double Latitude, double Longitude);
}
