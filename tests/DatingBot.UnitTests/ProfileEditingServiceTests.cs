using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace DatingBot.UnitTests;

public class ProfileEditingServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUserProfileRepository> _profileRepoMock = new();
    private readonly Mock<IInterestRepository> _interestRepoMock = new();
    private readonly Mock<ICityRepository> _cityRepoMock = new();
    private readonly Mock<IAiEmbeddingService> _aiEmbeddingMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ProfileEditingService _service;

    public ProfileEditingServiceTests()
    {
        _service = new ProfileEditingService(
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
    public async Task Should_UpdateNameAndResetStateToActive_When_NameIsValid()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_Name };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, Name = "Старое Имя" };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.UpdateNameAsync(telegramId, "Новое Имя");

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Name.Should().Be("Новое Имя");
        user.State.Should().Be(UserState.Active);
    }

    [Fact]
    public async Task Should_FailToUpdateName_When_NameIsInvalid()
    {
        // Arrange
        const long telegramId = 12345;

        // Act
        var result = await _service.UpdateNameAsync(telegramId, "A");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("минимум 2");
    }

    [Fact]
    public async Task Should_UpdateAgeAndReset18PlusTarget_When_AgeBecomesMinor()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_Age };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, Age = 22, DatingTarget = DatingTarget.AdultOnly };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.UpdateAgeAsync(telegramId, 15);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Age.Should().Be(15);
        profile.DatingTarget.Should().Be(DatingTarget.Relationship); // Сброшено с 18+
        user.State.Should().Be(UserState.Active);
    }

    [Fact]
    public async Task Should_UpdateCity_When_CityIsValid()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_City };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, City = "Казань" };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.UpdateCityAsync(telegramId, "Санкт-Петербург");

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.City.Should().Be("Санкт-Петербург");
        user.State.Should().Be(UserState.Active);
    }

    [Fact]
    public async Task Should_FailToUpdateCity_When_CityContainsDigits()
    {
        // Arrange
        const long telegramId = 12345;

        // Act
        var result = await _service.UpdateCityAsync(telegramId, "Москва2026");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("букв");
    }

    [Fact]
    public async Task Should_UpdateHeightAndAllowNull_When_HeightIsRemoved()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_Height };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, Height = 180 };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.UpdateHeightAsync(telegramId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Height.Should().BeNull();
        user.State.Should().Be(UserState.Active);
    }

    [Fact]
    public async Task Should_FailToUpdateDatingTarget_When_MinorSelectsAdultOnly()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_DatingTarget };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, Age = 16 };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.UpdateDatingTargetAsync(telegramId, DatingTarget.AdultOnly);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("18 лет");
    }

    [Fact]
    public async Task Should_CancelEditingAndSetStateActive_When_Cancelled()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_Photo };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.CancelEditingAsync(telegramId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.State.Should().Be(UserState.Active);
    }

    [Fact]
    public async Task Should_ToggleAgeCategories_When_CategoryClicked()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_SearchAgeCategories };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, AgeFilters = AgeCategoryFilter.None };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act 1: включить 18-25
        var result1 = await _service.ToggleAgeCategoryAsync(telegramId, AgeCategoryFilter.Age18To25);
        result1.IsSuccess.Should().BeTrue();
        profile.AgeFilters.HasFlag(AgeCategoryFilter.Age18To25).Should().BeTrue();

        // Act 2: включить 25-30
        var result2 = await _service.ToggleAgeCategoryAsync(telegramId, AgeCategoryFilter.Age25To30);
        result2.IsSuccess.Should().BeTrue();
        profile.AgeFilters.HasFlag(AgeCategoryFilter.Age18To25).Should().BeTrue();
        profile.AgeFilters.HasFlag(AgeCategoryFilter.Age25To30).Should().BeTrue();

        // Act 3: выключить 18-25
        var result3 = await _service.ToggleAgeCategoryAsync(telegramId, AgeCategoryFilter.Age18To25);
        result3.IsSuccess.Should().BeTrue();
        profile.AgeFilters.HasFlag(AgeCategoryFilter.Age18To25).Should().BeFalse();
        profile.AgeFilters.HasFlag(AgeCategoryFilter.Age25To30).Should().BeTrue();
    }

    [Fact]
    public async Task Should_SaveAgeCategoriesAndResetStateToActive()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_SearchAgeCategories };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.SaveAgeCategoriesAsync(telegramId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.State.Should().Be(UserState.Active);
    }

    [Fact]
    public async Task Should_SetSearchMinAgeAndTransitionToMaxAgeState_When_Valid()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_SearchMinAge };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, AgeFilters = AgeCategoryFilter.Age18To25 };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.SetSearchMinAgeAsync(telegramId, 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.SearchMinAge.Should().Be(20);
        profile.AgeFilters.Should().Be(AgeCategoryFilter.None); // Сброшено в пользу ручного диапазона
        user.State.Should().Be(UserState.Editing_SearchMaxAge);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(105)]
    public async Task Should_FailSetSearchMinAge_When_OutOfRange(int minAge)
    {
        // Act
        var result = await _service.SetSearchMinAgeAsync(12345, minAge);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("от 10 до 100");
    }

    [Fact]
    public async Task Should_SetSearchMaxAgeAndResetStateToActive_When_Valid()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_SearchMaxAge };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, SearchMinAge = 18 };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.SetSearchMaxAgeAsync(telegramId, 27);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.SearchMaxAge.Should().Be(27);
        user.State.Should().Be(UserState.Active);
    }

    [Fact]
    public async Task Should_FailSetSearchMaxAge_When_LessThanMinAge()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_SearchMaxAge };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, SearchMinAge = 25 };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.SetSearchMaxAgeAsync(telegramId, 20);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("не может быть меньше");
    }

    [Fact]
    public async Task Should_UpdateGreetingAndResetStateToActive_When_GreetingIsValid()
    {
        // Arrange
        const long telegramId = 12345;
        var user = new User { Id = Guid.NewGuid(), TelegramId = telegramId, State = UserState.Editing_Greeting };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, Greeting = "Старое приветствие" };

        _userRepoMock.Setup(r => r.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.UpdateGreetingAsync(telegramId, "Ищу людей для прогулок по парку");

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Greeting.Should().Be("Ищу людей для прогулок по парку");
        user.State.Should().Be(UserState.Active);
    }

    [Fact]
    public async Task Should_FailToUpdateGreeting_When_GreetingIsTooShort()
    {
        // Arrange
        const long telegramId = 12345;

        // Act
        var result = await _service.UpdateGreetingAsync(telegramId, "A");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("минимум 2");
    }

    [Fact]
    public async Task Should_FailToUpdateGreeting_When_GreetingExceedsMaxLength()
    {
        // Arrange
        const long telegramId = 12345;
        var longGreeting = new string('A', 301);

        // Act
        var result = await _service.UpdateGreetingAsync(telegramId, longGreeting);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("не должно превышать 300");
    }
}
