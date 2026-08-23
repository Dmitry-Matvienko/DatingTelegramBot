using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using DatingBot.Infrastructure.Data;
using DatingBot.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DatingBot.UnitTests;

public class UserProfileRepositoryAdminStatsTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        var cityMsk = new City { Id = 1, Name = "Москва", Country = "Россия" };
        var cityKbp = new City { Id = 2, Name = "Киев", Country = "Украина" };
        context.Cities.AddRange(cityMsk, cityKbp);

        var u1 = new User { Id = Guid.NewGuid(), TelegramId = 101, State = UserState.Active, CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var u2 = new User { Id = Guid.NewGuid(), TelegramId = 102, State = UserState.Active, CreatedAt = DateTime.UtcNow.AddHours(-10) };
        var u3 = new User { Id = Guid.NewGuid(), TelegramId = 103, State = UserState.Banned, CreatedAt = DateTime.UtcNow.AddDays(-2) };
        var u4 = new User { Id = Guid.NewGuid(), TelegramId = 104, State = UserState.Active, CreatedAt = DateTime.UtcNow.AddDays(-5) };
        context.Users.AddRange(u1, u2, u3, u4);

        var p1 = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = u1.Id,
            User = u1,
            City = "Москва",
            CityId = cityMsk.Id,
            CityRef = cityMsk,
            Gender = Gender.Male,
            Age = 25,
            DatingTarget = DatingTarget.Friends,
            IsCompleted = true
        };

        var p2 = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = u2.Id,
            User = u2,
            City = "Москва",
            CityId = cityMsk.Id,
            CityRef = cityMsk,
            Gender = Gender.Female,
            Age = 22,
            DatingTarget = DatingTarget.Relationship,
            IsCompleted = true
        };

        var p3 = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = u3.Id,
            User = u3,
            City = "Москва",
            CityId = cityMsk.Id,
            CityRef = cityMsk,
            Gender = Gender.Male,
            Age = 19,
            DatingTarget = DatingTarget.AdultOnly,
            IsCompleted = false
        };

        var p4 = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = u4.Id,
            User = u4,
            City = "Киев",
            CityId = cityKbp.Id,
            CityRef = cityKbp,
            Gender = Gender.Female,
            Age = 28,
            DatingTarget = DatingTarget.Relationship,
            IsCompleted = true
        };

        context.UserProfiles.AddRange(p1, p2, p3, p4);
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task GetTopCitiesStatsAsync_ShouldReturnGroupedCityStats()
    {
        using var context = CreateInMemoryDbContext();
        var repo = new UserProfileRepository(context);

        var stats = await repo.GetTopCitiesStatsAsync(5);

        stats.Should().NotBeEmpty();
        stats.Count.Should().Be(2);

        var moscow = stats.First(s => s.CityName == "Москва");
        moscow.UserCount.Should().Be(3);
        moscow.CompletedCount.Should().Be(2);
        moscow.MaleCount.Should().Be(2);
        moscow.FemaleCount.Should().Be(1);
        moscow.Country.Should().Be("Россия");

        var kyiv = stats.First(s => s.CityName == "Киев");
        kyiv.UserCount.Should().Be(1);
        kyiv.CompletedCount.Should().Be(1);
        kyiv.MaleCount.Should().Be(0);
        kyiv.FemaleCount.Should().Be(1);
        kyiv.Country.Should().Be("Украина");
    }

    [Fact]
    public async Task GetAdminStatsAsync_ShouldAggregateMetricsCorrectly()
    {
        using var context = CreateInMemoryDbContext();
        var repo = new UserProfileRepository(context);

        var stats = await repo.GetAdminStatsAsync();

        stats.TotalUsers.Should().Be(4);
        stats.CompletedProfiles.Should().Be(3);
        stats.BannedUsers.Should().Be(1);
        stats.MaleCount.Should().Be(2);
        stats.FemaleCount.Should().Be(2);
        stats.NewUsersLast24Hours.Should().Be(2);
        stats.NewUsersLast7Days.Should().Be(4);

        stats.DatingTargetFriendsCount.Should().Be(1);
        stats.DatingTargetRelationshipCount.Should().Be(2);
        stats.DatingTargetAdultOnlyCount.Should().Be(1);

        stats.TopCities.Should().HaveCount(2);
        stats.TopCountries.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCityStatsAsync_ShouldReturnSingleCityStats()
    {
        using var context = CreateInMemoryDbContext();
        var repo = new UserProfileRepository(context);

        var stats = await repo.GetCityStatsAsync("москва");

        stats.Should().NotBeNull();
        stats!.CityName.Should().Be("Москва");
        stats.Country.Should().Be("Россия");
        stats.UserCount.Should().Be(3);
        stats.CompletedCount.Should().Be(2);
        stats.MaleCount.Should().Be(2);
        stats.FemaleCount.Should().Be(1);
    }
}
