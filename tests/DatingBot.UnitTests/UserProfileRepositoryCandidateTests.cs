using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using DatingBot.Infrastructure.Data;
using DatingBot.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DatingBot.UnitTests;

public class UserProfileRepositoryCandidateTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetEligibleCandidatesAsync_ShouldExcludeSelf_And_ExcludeRatedAndReportedUsers()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repo = new UserProfileRepository(context);

        var city = new City { Id = 1, Name = "Москва", Country = "Россия" };
        context.Cities.Add(city);

        var currentUser = new User { Id = Guid.NewGuid(), TelegramId = 100, State = UserState.Searching, Language = AppLanguage.Russian };
        var candidateNormalUser = new User { Id = Guid.NewGuid(), TelegramId = 101, State = UserState.Active, Language = AppLanguage.Russian };
        var candidateRatedUser = new User { Id = Guid.NewGuid(), TelegramId = 102, State = UserState.Active, Language = AppLanguage.Russian };
        var candidateReportedUser = new User { Id = Guid.NewGuid(), TelegramId = 103, State = UserState.Active, Language = AppLanguage.Russian };
        context.Users.AddRange(currentUser, candidateNormalUser, candidateRatedUser, candidateReportedUser);

        var currentProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            User = currentUser,
            Gender = Gender.Male,
            TargetGender = TargetGender.Female,
            DatingTarget = DatingTarget.Relationship,
            CityId = city.Id,
            City = "Москва",
            CityRef = city,
            Age = 25,
            IsCompleted = true
        };

        var candidateNormalProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = candidateNormalUser.Id,
            User = candidateNormalUser,
            Gender = Gender.Female,
            TargetGender = TargetGender.Male,
            DatingTarget = DatingTarget.Relationship,
            CityId = city.Id,
            City = "Москва",
            CityRef = city,
            Age = 23,
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var candidateRatedProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = candidateRatedUser.Id,
            User = candidateRatedUser,
            Gender = Gender.Female,
            TargetGender = TargetGender.Male,
            DatingTarget = DatingTarget.Relationship,
            CityId = city.Id,
            City = "Москва",
            CityRef = city,
            Age = 24,
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        var candidateReportedProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = candidateReportedUser.Id,
            User = candidateReportedUser,
            Gender = Gender.Female,
            TargetGender = TargetGender.Male,
            DatingTarget = DatingTarget.Relationship,
            CityId = city.Id,
            City = "Москва",
            CityRef = city,
            Age = 22,
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        };

        context.UserProfiles.AddRange(currentProfile, candidateNormalProfile, candidateRatedProfile, candidateReportedProfile);

        // Оценка: текущий пользователь уже оценил candidateRatedUser
        context.ProfileRatings.Add(new ProfileRating
        {
            Id = Guid.NewGuid(),
            FromUserId = currentUser.Id,
            ToUserId = candidateRatedUser.Id,
            Score = 7,
            CreatedAt = DateTime.UtcNow
        });

        // Жалоба: текущий пользователь пожаловался на candidateReportedUser
        context.ProfileReports.Add(new ProfileReport
        {
            Id = Guid.NewGuid(),
            ReporterId = currentUser.Id,
            ReportedUserId = candidateReportedUser.Id,
            Reason = ReportReason.InappropriateContent,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        // Act
        var eligible = await repo.GetEligibleCandidatesAsync(currentProfile, limit: 100);

        // Assert
        eligible.Should().HaveCount(1);
        eligible[0].UserId.Should().Be(candidateNormalUser.Id);
    }

    [Fact]
    public async Task GetEligibleCandidatesAsync_ShouldRespectLimit_And_OrderByMostRecent()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repo = new UserProfileRepository(context);

        var city = new City { Id = 1, Name = "Москва", Country = "Россия" };
        context.Cities.Add(city);

        var currentUser = new User { Id = Guid.NewGuid(), TelegramId = 1000, State = UserState.Searching, Language = AppLanguage.Russian };
        context.Users.Add(currentUser);

        var currentProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            User = currentUser,
            Gender = Gender.Male,
            TargetGender = TargetGender.Female,
            DatingTarget = DatingTarget.Friends,
            CityId = city.Id,
            City = "Москва",
            CityRef = city,
            Age = 25,
            IsCompleted = true
        };
        context.UserProfiles.Add(currentProfile);

        // Добавляем 150 подходящих кандидатов
        var baseTime = DateTime.UtcNow;
        for (var i = 1; i <= 150; i++)
        {
            var candidateUser = new User
            {
                Id = Guid.NewGuid(),
                TelegramId = 1000 + i,
                State = UserState.Active,
                Language = AppLanguage.Russian
            };
            context.Users.Add(candidateUser);

            var candidateProfile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = candidateUser.Id,
                User = candidateUser,
                Gender = Gender.Female,
                TargetGender = TargetGender.Male,
                DatingTarget = DatingTarget.Friends,
                CityId = city.Id,
                City = "Москва",
                CityRef = city,
                Age = 20,
                IsCompleted = true,
                CreatedAt = baseTime.AddMinutes(i) // Самый свежий кандидат будет с i = 150
            };
            context.UserProfiles.Add(candidateProfile);
        }

        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetEligibleCandidatesAsync(currentProfile, limit: 100);

        // Assert
        result.Should().HaveCount(100);
        // Первый кандидат должен быть самым свежим (созданным последним)
        result.First().CreatedAt.Should().BeAfter(result.Last().CreatedAt);
    }

    [Fact]
    public async Task GetIncomingUnratedHighRatingsAsync_ShouldExcludeUsersAlreadyRatedBack()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var repo = new ProfileRatingRepository(context);

        var me = new User { Id = Guid.NewGuid(), TelegramId = 500, State = UserState.Active };
        var rater1 = new User { Id = Guid.NewGuid(), TelegramId = 501, State = UserState.Active };
        var rater2 = new User { Id = Guid.NewGuid(), TelegramId = 502, State = UserState.Active };
        context.Users.AddRange(me, rater1, rater2);

        // rater1 оценил me на 8
        var rating1 = new ProfileRating
        {
            Id = Guid.NewGuid(),
            FromUserId = rater1.Id,
            FromUser = rater1,
            ToUserId = me.Id,
            Score = 8,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        // rater2 оценил me на 9
        var rating2 = new ProfileRating
        {
            Id = Guid.NewGuid(),
            FromUserId = rater2.Id,
            FromUser = rater2,
            ToUserId = me.Id,
            Score = 9,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        // me уже оценил rater2 обратно на 7
        var backRating = new ProfileRating
        {
            Id = Guid.NewGuid(),
            FromUserId = me.Id,
            FromUser = me,
            ToUserId = rater2.Id,
            Score = 7,
            CreatedAt = DateTime.UtcNow
        };

        context.ProfileRatings.AddRange(rating1, rating2, backRating);
        await context.SaveChangesAsync();

        // Act
        var unratedIncoming = await repo.GetIncomingUnratedHighRatingsAsync(me.Id);

        // Assert
        unratedIncoming.Should().HaveCount(1);
        unratedIncoming[0].FromUserId.Should().Be(rater1.Id);
        unratedIncoming[0].Score.Should().Be(8);
    }

    [Fact]
    public void ProfileRating_ShouldHave_CompositeIndex_On_ToUser_Score_CreatedAt()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        // Act
        var entityType = context.Model.FindEntityType(typeof(ProfileRating));
        var indexes = entityType?.GetIndexes().ToList();

        // Assert
        indexes.Should().NotBeNull();
        var compositeIndex = indexes!.FirstOrDefault(i => i.GetDatabaseName() == "IX_ProfileRatings_ToUser_Score_CreatedAt");
        compositeIndex.Should().NotBeNull();
        compositeIndex!.Properties.Select(p => p.Name).Should().ContainInOrder(nameof(ProfileRating.ToUserId), nameof(ProfileRating.Score), nameof(ProfileRating.CreatedAt));
    }
}
