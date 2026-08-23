using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace DatingBot.UnitTests;

public class SearchServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUserProfileRepository> _profileRepoMock = new();
    private readonly Mock<IInterestRepository> _interestRepoMock = new();
    private readonly Mock<IProfileRatingRepository> _ratingRepoMock = new();
    private readonly Mock<IProfileReportRepository> _reportRepoMock = new();
    private readonly Mock<IMatchmakingService> _matchmakingMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly SearchService _service;

    public SearchServiceTests()
    {
        _service = new SearchService(
            _userRepoMock.Object,
            _profileRepoMock.Object,
            _interestRepoMock.Object,
            _ratingRepoMock.Object,
            _reportRepoMock.Object,
            _matchmakingMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Should_ReturnCandidate_When_MatchingCandidateExists()
    {
        // Arrange
        const long telegramId = 11111;
        var candidateDto = new UserProfileDto(
            Guid.NewGuid(),
            22222,
            "anna",
            Gender.Female,
            TargetGender.Male,
            "Анна",
            22,
            "Москва",
            168,
            "photo_123",
            DatingTarget.Relationship,
            null,
            [],
            true,
            RatingCount: 2,
            AverageRating: 8.5
        );

        var matchDto = new MatchCandidateDto(
            candidateDto,
            [],
            [],
            MatchTier.SameCity,
            "📍 Собеседник из вашего города"
        );

        _matchmakingMock.Setup(m => m.GetNextMatchCandidateAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchDto);

        // Act
        var result = await _service.GetNextCandidateAsync(telegramId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Анна");
        result.TelegramId.Should().Be(22222);
        result.AverageRating.Should().Be(8.5);
        result.RatingCount.Should().Be(2);
    }

    [Fact]
    public async Task Should_ReturnNull_When_NoMatchCandidatesFound()
    {
        // Arrange
        const long telegramId = 11111;
        _matchmakingMock.Setup(m => m.GetNextMatchCandidateAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchCandidateDto?)null);

        // Act
        var result = await _service.GetNextCandidateAsync(telegramId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_RateCandidateAndRecalculateAverageRating()
    {
        // Arrange
        const long raterTelegramId = 11111;
        var rater = new User { Id = Guid.NewGuid(), TelegramId = raterTelegramId };

        var candidateUser = new User { Id = Guid.NewGuid(), TelegramId = 22222 };
        var candidateProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = candidateUser.Id,
            User = candidateUser,
            RatingCount = 1,
            AverageRating = 6.0
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(raterTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rater);
        _profileRepoMock.Setup(r => r.GetByIdAsync(candidateProfile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidateProfile);
        _ratingRepoMock.Setup(r => r.HasRatedAsync(rater.Id, candidateUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act (оцениваем на 10 баллов: (6.0 * 1 + 10) / 2 = 8.0)
        var result = await _service.RateCandidateAsync(raterTelegramId, candidateProfile.Id, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ToTelegramId.Should().Be(22222);
        result.Value.Score.Should().Be(10);
        result.Value.NewRatingCount.Should().Be(2);
        result.Value.NewAverageRating.Should().Be(8.0);

        candidateProfile.RatingCount.Should().Be(2);
        candidateProfile.AverageRating.Should().Be(8.0);
        _ratingRepoMock.Verify(r => r.AddAsync(It.Is<ProfileRating>(pr => pr.FromUserId == rater.Id && pr.ToUserId == candidateUser.Id && pr.Score == 10), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-5)]
    public async Task Should_FailToRateCandidate_When_ScoreIsOutOfRange(int score)
    {
        // Act
        var result = await _service.RateCandidateAsync(11111, Guid.NewGuid(), score);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("от 1 до 10");
    }

    [Fact]
    public async Task Should_FailToRateSelf()
    {
        // Arrange
        const long raterTelegramId = 11111;
        var rater = new User { Id = Guid.NewGuid(), TelegramId = raterTelegramId };
        var candidateProfile = new UserProfile { Id = Guid.NewGuid(), UserId = rater.Id, User = rater };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(raterTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rater);
        _profileRepoMock.Setup(r => r.GetByIdAsync(candidateProfile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidateProfile);

        // Act
        var result = await _service.RateCandidateAsync(raterTelegramId, candidateProfile.Id, 9);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("самого себя");
    }

    [Fact]
    public async Task Should_CreateReportSuccessfully()
    {
        // Arrange
        const long reporterTelegramId = 11111;
        var reporter = new User { Id = Guid.NewGuid(), TelegramId = reporterTelegramId, Username = "ivan", FirstName = "Иван" };

        var candidateUser = new User { Id = Guid.NewGuid(), TelegramId = 22222, Username = "bad_user" };
        var candidateProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = candidateUser.Id,
            User = candidateUser,
            Name = "Нарушитель",
            Age = 20,
            PhotoFileId = "photo_123"
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(reporterTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reporter);
        _profileRepoMock.Setup(r => r.GetByIdAsync(candidateProfile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidateProfile);

        // Act
        var result = await _service.ReportCandidateAsync(reporterTelegramId, candidateProfile.Id, ReportReason.Other, "Спам и реклама");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReportId.Should().NotBeEmpty();
        result.Value.ReportedProfile.TelegramId.Should().Be(22222);
        result.Value.ReportedProfile.Name.Should().Be("Нарушитель");
        result.Value.Reason.Should().Be(ReportReason.Other);
        result.Value.Details.Should().Be("Спам и реклама");
        result.Value.ReporterTelegramId.Should().Be(reporterTelegramId);

        _reportRepoMock.Verify(r => r.AddAsync(It.Is<ProfileReport>(pr => pr.ReporterId == reporter.Id && pr.ReportedUserId == candidateUser.Id && pr.Reason == ReportReason.Other && pr.Details == "Спам и реклама"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_DetectMutualMatch_When_BothRate6OrHigher()
    {
        // Arrange
        const long raterTelegramId = 11111;
        var rater = new User { Id = Guid.NewGuid(), TelegramId = raterTelegramId, Username = "bob", FirstName = "Боб" };
        var raterProfile = new UserProfile { Id = Guid.NewGuid(), UserId = rater.Id, User = rater, Name = "Боб", Age = 25 };

        var candidateUser = new User { Id = Guid.NewGuid(), TelegramId = 22222, Username = "alice", FirstName = "Алиса" };
        var candidateProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = candidateUser.Id,
            User = candidateUser,
            Name = "Алиса",
            Age = 23,
            RatingCount = 1,
            AverageRating = 7.0
        };

        var candidatePreviousRating = new ProfileRating
        {
            Id = Guid.NewGuid(),
            FromUserId = candidateUser.Id,
            ToUserId = rater.Id,
            Score = 8
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(raterTelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rater);
        _profileRepoMock.Setup(r => r.GetByIdAsync(candidateProfile.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidateProfile);
        _profileRepoMock.Setup(r => r.GetWithInterestsByUserIdAsync(rater.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(raterProfile);
        _ratingRepoMock.Setup(r => r.HasRatedAsync(rater.Id, candidateUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ratingRepoMock.Setup(r => r.GetRatingAsync(candidateUser.Id, rater.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidatePreviousRating);
        _interestRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Interest>());

        // Act (Боб в ответ ставит Алисе 9/10)
        var result = await _service.RateCandidateAsync(raterTelegramId, candidateProfile.Id, 9);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsMutualMatch.Should().BeTrue();
        result.Value.OriginalScore.Should().Be(8);
        result.Value.Score.Should().Be(9);
        result.Value.RaterProfile.Should().NotBeNull();
        result.Value.RaterProfile!.Name.Should().Be("Боб");
        result.Value.CandidateProfile.Should().NotBeNull();
        result.Value.CandidateProfile!.Name.Should().Be("Алиса");
    }

    [Fact]
    public async Task Should_ReturnIncomingRatings_When_UserWasRatedHigh()
    {
        // Arrange
        const long telegramId = 11111;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId };

        var raterUser = new User { Id = Guid.NewGuid(), TelegramId = 22222, Username = "kate" };
        var raterProfile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = raterUser.Id,
            User = raterUser,
            Name = "Катя",
            Age = 21,
            IsCompleted = true
        };

        var incomingRating = new ProfileRating
        {
            Id = Guid.NewGuid(),
            FromUserId = raterUser.Id,
            ToUserId = user.Id,
            Score = 9,
            CreatedAt = DateTime.UtcNow
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRatingRepositoryMock_Setup(user.Id, incomingRating, raterProfile);

        // Act
        var result = await _service.GetNextIncomingRatingAsync(telegramId);

        // Assert
        result.Should().NotBeNull();
        result!.ScoreReceived.Should().Be(9);
        result.RaterProfile.Name.Should().Be("Катя");
        result.RaterProfile.TelegramId.Should().Be(22222);
    }

    private void _profileRatingRepositoryMock_Setup(Guid userId, ProfileRating rating, UserProfile raterProfile)
    {
        _ratingRepoMock.Setup(r => r.GetIncomingUnratedHighRatingsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProfileRating> { rating });
        _profileRepoMock.Setup(r => r.GetWithInterestsByUserIdAsync(rating.FromUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(raterProfile);
        _interestRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Interest>());
    }
}
