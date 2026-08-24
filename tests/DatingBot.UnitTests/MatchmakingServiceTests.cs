using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace DatingBot.UnitTests;

public class MatchmakingServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUserProfileRepository> _profileRepoMock = new();
    private readonly Mock<IInterestRepository> _interestRepoMock = new();
    private readonly Mock<IAiEmbeddingService> _aiEmbeddingMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly MatchmakingService _service;

    public MatchmakingServiceTests()
    {
        _service = new MatchmakingService(
            _userRepoMock.Object,
            _profileRepoMock.Object,
            _interestRepoMock.Object,
            _aiEmbeddingMock.Object,
            new LocalizationService(),
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Should_Prioritize_Tier1_AiCompatibility_When_SimilarityAboveThreshold()
    {
        // Arrange
        const long telegramId = 11111;
        var currentUser = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Active };
        var moscowCity = new City { Id = 1, Name = "Москва", Latitude = 55.7558, Longitude = 37.6173 };

        var currentProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            Gender = Gender.Male,
            TargetGender = TargetGender.Female,
            City = "Москва",
            CityId = 1,
            CityRef = moscowCity,
            IsCompleted = true,
            AiVector = [1, 2, 3]
        };

        // Кандидат 1: Сходство ИИ 0.85 (Tier 1)
        var candidate1 = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), TelegramId = 22222, Username = "ai_match" },
            Name = "Елена (ИИ Мэтч)",
            Age = 23,
            City = "Москва",
            CityId = 1,
            CityRef = moscowCity,
            IsCompleted = true,
            AiVector = [4, 5, 6]
        };

        // Кандидат 2: 3 общих интереса, но сходство ИИ 0.30 (Tier 2)
        var candidate2 = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), TelegramId = 33333, Username = "interests_match" },
            Name = "Ольга (Интересы)",
            Age = 24,
            City = "Москва",
            CityId = 1,
            CityRef = moscowCity,
            IsCompleted = true,
            AiVector = [7, 8, 9]
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _profileRepoMock.Setup(r => r.GetWithInterestsByUserIdAsync(currentUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProfile);

        _profileRepoMock.Setup(r => r.GetEligibleCandidatesAsync(currentProfile, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { candidate2, candidate1 });

        _aiEmbeddingMock.Setup(a => a.BytesToVector(It.IsAny<byte[]>()))
            .Returns<byte[]>(b => new float[] { b[0] });

        // candidate1 сходство 0.85 (> 0.55), candidate2 сходство 0.30
        _aiEmbeddingMock.Setup(a => a.CalculateCosineSimilarity(It.IsAny<float[]>(), candidate1.AiVector!))
            .Returns(0.85);
        _aiEmbeddingMock.Setup(a => a.CalculateCosineSimilarity(It.IsAny<float[]>(), candidate2.AiVector!))
            .Returns(0.30);

        _interestRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Interest>());

        // Act
        var match = await _service.GetNextMatchCandidateAsync(telegramId);

        // Assert
        match.Should().NotBeNull();
        match!.Profile.Name.Should().Be("Елена (ИИ Мэтч)");
        match.Tier.Should().Be(MatchTier.AiCompatibility);
        match.MatchReasonBadge.Should().Contain("ИИ-анализа");
        currentUser.State.Should().Be(UserState.Searching);
        currentUser.CurrentCandidateProfileId.Should().Be(candidate1.Id);
    }

    [Fact]
    public async Task Should_Prioritize_Tier2_CommonInterests_When_NoAiMatch()
    {
        // Arrange
        const long telegramId = 11111;
        var currentUser = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Active };
        var moscowCity = new City { Id = 1, Name = "Москва" };

        var interestMusic = new Interest { Id = 1, Code = InterestType.Music, Title = "Музыка", Icon = "🎵" };
        var interestGames = new Interest { Id = 2, Code = InterestType.Gaming, Title = "Видеоигры", Icon = "🎮" };

        var currentProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            City = "Москва",
            CityId = 1,
            CityRef = moscowCity,
            IsCompleted = true,
            Interests =
            [
                new UserProfileInterest { InterestId = 1, Interest = interestMusic },
                new UserProfileInterest { InterestId = 2, Interest = interestGames }
            ]
        };

        // Кандидат 1: 2 общих интереса
        var candidate1 = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), TelegramId = 22222 },
            Name = "Мария",
            City = "Москва",
            CityId = 1,
            CityRef = moscowCity,
            IsCompleted = true,
            Interests =
            [
                new UserProfileInterest { InterestId = 1, Interest = interestMusic },
                new UserProfileInterest { InterestId = 2, Interest = interestGames }
            ]
        };

        // Кандидат 2: 0 общих интересов
        var candidate2 = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), TelegramId = 33333 },
            Name = "Дарья",
            City = "Москва",
            CityId = 1,
            CityRef = moscowCity,
            IsCompleted = true,
            Interests = []
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _profileRepoMock.Setup(r => r.GetWithInterestsByUserIdAsync(currentUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProfile);

        _profileRepoMock.Setup(r => r.GetEligibleCandidatesAsync(currentProfile, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { candidate2, candidate1 });

        _interestRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Interest> { interestMusic, interestGames });

        // Act
        var match = await _service.GetNextMatchCandidateAsync(telegramId);

        // Assert
        match.Should().NotBeNull();
        match!.Profile.Name.Should().Be("Мария");
        match.Tier.Should().Be(MatchTier.CommonInterests);
        match.CommonInterests.Should().HaveCount(2);
        match.MatchReasonBadge.Should().Contain("общих интереса");
    }

    [Fact]
    public async Task Should_Prioritize_Tier4_NearbyCity_SortedByDistance_When_SameCityPoolExhausted()
    {
        // Arrange
        const long telegramId = 11111;
        var currentUser = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Active };
        var moscowCity = new City { Id = 1, Name = "Москва", Latitude = 55.7558, Longitude = 37.6173 };
        var khimkiCity = new City { Id = 2, Name = "Химки", Latitude = 55.8970, Longitude = 37.4297 }; // ~19 км
        var tverCity = new City { Id = 42, Name = "Тверь", Latitude = 56.8587, Longitude = 35.9176 }; // ~160 км

        var currentProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            City = "Москва",
            CityId = 1,
            CityRef = moscowCity,
            IsCompleted = true
        };

        var candidateTver = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), TelegramId = 22222 },
            Name = "Светлана (Тверь)",
            City = "Тверь",
            CityId = 42,
            CityRef = tverCity,
            IsCompleted = true
        };

        var candidateKhimki = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), TelegramId = 33333 },
            Name = "Виктория (Химки)",
            City = "Химки",
            CityId = 2,
            CityRef = khimkiCity,
            IsCompleted = true
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _profileRepoMock.Setup(r => r.GetWithInterestsByUserIdAsync(currentUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProfile);

        // Передаем Тверь первым в списке, но Химки ближе
        _profileRepoMock.Setup(r => r.GetEligibleCandidatesAsync(currentProfile, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { candidateTver, candidateKhimki });

        _interestRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Interest>());

        // Act
        var match = await _service.GetNextMatchCandidateAsync(telegramId);

        // Assert
        match.Should().NotBeNull();
        match!.Profile.Name.Should().Be("Виктория (Химки)");
        match.Tier.Should().Be(MatchTier.NearbyCity);
        match.DistanceKm.Should().BeLessThan(30);
        match.MatchReasonBadge.Should().Contain("Химки");
    }

    [Fact]
    public async Task Should_FilterOut_NearbyCity_When_DistanceExceeds_500Km()
    {
        // Arrange
        const long telegramId = 11111;
        var currentUser = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Active };
        var moscowCity = new City { Id = 1, Name = "Москва", Latitude = 55.7558, Longitude = 37.6173 };
        // Владивосток (> 6400 км от Москвы)
        var vladivostokCity = new City { Id = 999, Name = "Владивосток", Latitude = 43.1155, Longitude = 131.8855 };

        var currentProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            City = "Москва",
            CityId = 1,
            CityRef = moscowCity,
            IsCompleted = true
        };

        var candidateFar = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), TelegramId = 22222 },
            Name = "Анастасия (Владивосток)",
            City = "Владивосток",
            CityId = 999,
            CityRef = vladivostokCity,
            IsCompleted = true
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _profileRepoMock.Setup(r => r.GetWithInterestsByUserIdAsync(currentUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProfile);

        _profileRepoMock.Setup(r => r.GetEligibleCandidatesAsync(currentProfile, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { candidateFar });

        // Act
        var match = await _service.GetNextMatchCandidateAsync(telegramId);

        // Assert
        match.Should().BeNull(); // Должен вернуть null, т.к. > 500 км
    }

    [Fact]
    public async Task Should_ResetHistoryForCity_Successfully()
    {
        // Arrange
        const long telegramId = 11111;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, CityId = 1, City = "Москва" };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _profileRepoMock.Setup(r => r.ResetRatingsForCityAsync(user.Id, 1, "Москва", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _service.ResetHistoryForCityAsync(telegramId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.SearchCycleStartedAt.Should().NotBeNull();
        _userRepoMock.Verify(r => r.Update(user), Times.Once);
    }

    [Fact]
    public async Task Should_AutomaticallyCycleCandidates_When_CurrentCycleExhausted()
    {
        // Arrange
        const long telegramId = 11111;
        var currentUser = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Active };
        var moscowCity = new City { Id = 1, Name = "Москва" };

        var currentProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            User = currentUser,
            Gender = Gender.Male,
            TargetGender = TargetGender.Female,
            DatingTarget = DatingTarget.Relationship,
            City = "Москва",
            CityId = 1,
            CityRef = moscowCity,
            IsCompleted = true
        };

        var candidate1 = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            User = new User { Id = Guid.NewGuid(), TelegramId = 22222 },
            Name = "Анна",
            Gender = Gender.Female,
            TargetGender = TargetGender.Male,
            DatingTarget = DatingTarget.Relationship,
            City = "Москва",
            CityId = 1,
            CityRef = moscowCity,
            IsCompleted = true
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _profileRepoMock.Setup(r => r.GetWithInterestsByUserIdAsync(currentUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProfile);

        // Первый вызов GetEligibleCandidatesAsync возвращает пустой список (все анкеты в текущем круге оценены)
        // Второй вызов (после перезапуска цикла) возвращает candidate1
        var callCount = 0;
        _profileRepoMock.Setup(r => r.GetEligibleCandidatesAsync(currentProfile, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? new List<UserProfile>() : new List<UserProfile> { candidate1 };
            });

        _profileRepoMock.Setup(r => r.GetTotalEligibleCandidatesCountAsync(currentProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _interestRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Interest>());

        // Act
        var match = await _service.GetNextMatchCandidateAsync(telegramId);

        // Assert
        match.Should().NotBeNull();
        match!.Profile.Name.Should().Be("Анна");
        currentUser.SearchCycleStartedAt.Should().NotBeNull();
        currentUser.CurrentCandidateProfileId.Should().Be(candidate1.Id);
        _profileRepoMock.Verify(r => r.GetTotalEligibleCandidatesCountAsync(currentProfile, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnNull_When_TotalEligibleCandidatesCountIsZero()
    {
        // Arrange
        const long telegramId = 11111;
        var currentUser = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Active };
        var currentProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = currentUser.Id,
            User = currentUser,
            Gender = Gender.Male,
            TargetGender = TargetGender.Female,
            DatingTarget = DatingTarget.AdultOnly,
            IsCompleted = true
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUser);
        _profileRepoMock.Setup(r => r.GetWithInterestsByUserIdAsync(currentUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProfile);

        _profileRepoMock.Setup(r => r.GetEligibleCandidatesAsync(currentProfile, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile>());

        _profileRepoMock.Setup(r => r.GetTotalEligibleCandidatesCountAsync(currentProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var match = await _service.GetNextMatchCandidateAsync(telegramId);

        // Assert
        match.Should().BeNull();
        currentUser.CurrentCandidateProfileId.Should().BeNull();
    }

    [Fact]
    public async Task Should_MatchMinorAndAdult_When_BothHaveRelationshipTargetAndNoAgeFilters()
    {
        // Arrange: Настя (15 лет, Девушка ищет Парня, Отношения) и Влад (22 года, Парень ищет Девушку, Отношения)
        const long nastyaTelegramId = 151515;
        var vologda = new City { Id = 35, Name = "Вологда" };

        var nastyaUser = new User { Id = Guid.NewGuid(), TelegramId = nastyaTelegramId, State = UserState.Searching };
        var nastyaProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = nastyaUser.Id,
            User = nastyaUser,
            Name = "Настя",
            Age = 15,
            Gender = Gender.Female,
            TargetGender = TargetGender.Male,
            DatingTarget = DatingTarget.Relationship,
            City = "Вологда",
            CityId = 35,
            CityRef = vologda,
            IsCompleted = true
        };

        var vladUser = new User { Id = Guid.NewGuid(), TelegramId = 222222, State = UserState.Searching };
        var vladProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = vladUser.Id,
            User = vladUser,
            Name = "Влад",
            Age = 22,
            Gender = Gender.Male,
            TargetGender = TargetGender.Female,
            DatingTarget = DatingTarget.Relationship,
            City = "Вологда",
            CityId = 35,
            CityRef = vologda,
            IsCompleted = true
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(nastyaTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nastyaUser);
        _profileRepoMock.Setup(r => r.GetWithInterestsByUserIdAsync(nastyaUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nastyaProfile);

        _profileRepoMock.Setup(r => r.GetEligibleCandidatesAsync(nastyaProfile, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProfile> { vladProfile });

        _interestRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Interest>());

        // Act
        var match = await _service.GetNextMatchCandidateAsync(nastyaTelegramId);

        // Assert: Настя успешно находит Влада
        match.Should().NotBeNull();
        match!.Profile.Name.Should().Be("Влад");
        match.Profile.Age.Should().Be(22);
    }
}
