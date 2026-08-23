using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace DatingBot.UnitTests;

public class RegistrationServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUserProfileRepository> _profileRepoMock = new();
    private readonly Mock<IInterestRepository> _interestRepoMock = new();
    private readonly Mock<ICityRepository> _cityRepoMock = new();
    private readonly Mock<IAiEmbeddingService> _aiEmbeddingMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RegistrationService _service;

    public RegistrationServiceTests()
    {
        _service = new RegistrationService(
            _userRepoMock.Object,
            _profileRepoMock.Object,
            _interestRepoMock.Object,
            _cityRepoMock.Object,
            _aiEmbeddingMock.Object,
            new LocalizationService(),
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Should_TransitionTo_SelectingTargetGender_When_GenderIsSet()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Registration_SelectingGender };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.SetGenderAsync(telegramId, Gender.Male);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Gender.Should().Be(Gender.Male);
        user.State.Should().Be(UserState.Registration_SelectingTargetGender);
    }

    [Fact]
    public async Task Should_TransitionTo_WaitingForName_When_TargetGenderIsSet()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Registration_SelectingTargetGender };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.SetTargetGenderAsync(telegramId, TargetGender.Female);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.TargetGender.Should().Be(TargetGender.Female);
        user.State.Should().Be(UserState.Registration_WaitingForName);
    }

    [Fact]
    public async Task Should_TransitionTo_WaitingForPhoto_When_HeightIsSkipped()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Registration_WaitingForHeight };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, Height = 190 };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.SkipHeightAsync(telegramId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Height.Should().BeNull();
        user.State.Should().Be(UserState.Registration_WaitingForPhoto);
    }

    [Fact]
    public async Task Should_ToggleInterestCorrectly_When_InterestIsClicked()
    {
        // Arrange
        const long telegramId = 12345;
        var profileId = Guid.NewGuid();
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId };
        var interest = new Interest { Id = 1, Code = InterestType.Gaming, Title = "Видеоигры", Icon = "🎮" };
        var profile = new UserProfile
        {
            Id = profileId,
            UserId = user.Id,
            User = user,
            Interests = []
        };

        _interestRepoMock.Setup(r => r.GetByCodeAsync(InterestType.Gaming, It.IsAny<CancellationToken>()))
            .ReturnsAsync(interest);
        _interestRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([interest]);
        _profileRepoMock.Setup(r => r.GetWithInterestsByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act - First Toggle (Select)
        var result1 = await _service.ToggleInterestAsync(telegramId, InterestType.Gaming);

        // Assert 1
        result1.IsSuccess.Should().BeTrue();
        result1.Value.Should().ContainSingle(i => i.Code == InterestType.Gaming && i.IsSelected);
    }

    [Fact]
    public async Task Should_CompleteRegistrationAndSetStateActive_When_AiDescriptionIsValid()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Registration_WaitingForAiBio };
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Иван",
            Age = 25,
            City = "Москва",
            Gender = Gender.Male,
            TargetGender = TargetGender.Female,
            DatingTarget = DatingTarget.Relationship,
            User = user
        };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetWithInterestsByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _interestRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.SetAiDescriptionAndCompleteAsync(telegramId, "Люблю спорт, прогулки и хорошую музыку.");

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.IsCompleted.Should().BeTrue();
        profile.AiDescription.Should().Be("Люблю спорт, прогулки и хорошую музыку.");
        user.State.Should().Be(UserState.Active);
    }

    [Fact]
    public async Task Should_Fail_When_MinorSelectsAdultOnly()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Registration_SelectingTarget };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, Age = 16 };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.SetDatingTargetAsync(telegramId, DatingTarget.AdultOnly);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("18 лет");
        user.State.Should().Be(UserState.Registration_SelectingTarget);
    }

    [Fact]
    public async Task Should_Succeed_When_AdultSelectsAdultOnly()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Registration_SelectingTarget };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, Age = 21 };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.SetDatingTargetAsync(telegramId, DatingTarget.AdultOnly);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.DatingTarget.Should().Be(DatingTarget.AdultOnly);
        user.State.Should().Be(UserState.Registration_WaitingForAiBio);
    }
}
