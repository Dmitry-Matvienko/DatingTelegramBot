using DatingBot.Domain.Entities;
using DatingBot.Infrastructure.Data;
using DatingBot.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DatingBot.UnitTests;

public class CityRepositoryTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Cities.AddRange(
            new City { Id = 1, Name = "Москва", Country = "Россия", Latitude = 55.7558, Longitude = 37.6173 },
            new City { Id = 2, Name = "Санкт-Петербург", Country = "Россия", Latitude = 59.9343, Longitude = 30.3351 },
            new City { Id = 3, Name = "Киев", Country = "Украина", Latitude = 50.4501, Longitude = 30.5234 },
            new City { Id = 4, Name = "Минск", Country = "Беларусь", Latitude = 53.9006, Longitude = 27.5590 },
            new City { Id = 5, Name = "Химки", Country = "Россия", Latitude = 55.8970, Longitude = 37.4297 },
            new City { Id = 6, Name = "Париж", Country = "Франция", Latitude = 48.8566, Longitude = 2.3522 }
        );
        context.SaveChanges();
        return context;
    }

    [Theory]
    [InlineData("Москва", "Москва")]
    [InlineData("мск", "Москва")]
    [InlineData("питер", "Санкт-Петербург")]
    [InlineData("спб", "Санкт-Петербург")]
    [InlineData("київ", "Киев")]
    public async Task FindExactByNameAsync_ShouldResolveExactAndSynonyms(string input, string expectedName)
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repository = new CityRepository(context);

        // Act
        var city = await repository.FindExactByNameAsync(input);

        // Assert
        city.Should().NotBeNull();
        city!.Name.Should().Be(expectedName);
    }

    [Fact]
    public async Task SearchSuggestionsAsync_ShouldReturnPrefixMatches()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repository = new CityRepository(context);

        // Act
        var suggestions = await repository.SearchSuggestionsAsync("Мос");

        // Assert
        suggestions.Should().NotBeEmpty();
        suggestions.First().Name.Should().Be("Москва");
    }

    [Fact]
    public async Task SearchSuggestionsAsync_ShouldFuzzyMatchTypos()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repository = new CityRepository(context);

        // Act (опечатка "Москв")
        var suggestions = await repository.SearchSuggestionsAsync("Москв");

        // Assert
        suggestions.Should().NotBeEmpty();
        suggestions.First().Name.Should().Be("Москва");
    }

    [Fact]
    public async Task SearchSuggestionsAsync_ShouldReturnUpToLimitSuggestions()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        context.Cities.AddRange(
            new City { Id = 10, Name = "Можайск", Country = "Россия", Latitude = 55.5, Longitude = 36.0 },
            new City { Id = 11, Name = "Моздок", Country = "Россия", Latitude = 43.7, Longitude = 44.6 },
            new City { Id = 12, Name = "Моршанск", Country = "Россия", Latitude = 53.4, Longitude = 41.8 }
        );
        context.SaveChanges();

        var repository = new CityRepository(context);

        // Act
        var suggestions = await repository.SearchSuggestionsAsync("Мо", limit: 3);

        // Assert
        suggestions.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddAsync_ShouldAddNewCityToDatabase()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repository = new CityRepository(context);

        var newCity = new City
        {
            Name = "Серпухов",
            Region = "Московская область",
            Country = "Россия",
            Latitude = 54.9167,
            Longitude = 37.4167
        };

        // Act
        var added = await repository.AddAsync(newCity);

        // Assert
        added.Id.Should().BeGreaterThan(0);
        var found = await repository.FindExactByNameAsync("Серпухов");
        found.Should().NotBeNull();
        found!.Region.Should().Be("Московская область");
    }

    [Fact]
    public void CalculateHaversineDistance_ShouldCalculateAccurateDistanceBetweenMoscowAndKhimki()
    {
        // Arrange: Москва -> Химки (~19 км)
        double lat1 = 55.7558, lon1 = 37.6173;
        double lat2 = 55.8970, lon2 = 37.4297;

        // Act
        var distance = CityRepository.CalculateHaversineDistance(lat1, lon1, lat2, lon2);

        // Assert
        distance.Should().BeInRange(15, 25);
    }
}
