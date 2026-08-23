using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace DatingBot.UnitTests;

public class AdminServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepo = new();
    private readonly Mock<IProfileReportRepository> _reportRepo = new();
    private readonly Mock<IInterestRepository> _interestRepo = new();
    private readonly Mock<IPaymentTransactionRepository> _paymentRepo = new();
    private readonly Mock<IAdminSettings> _adminSettings = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly AdminService _sut;

    public AdminServiceTests()
    {
        _adminSettings.Setup(s => s.AdminIds).Returns([123456789, 987654321]);

        _sut = new AdminService(
            _userRepo.Object,
            _userProfileRepo.Object,
            _reportRepo.Object,
            _interestRepo.Object,
            _paymentRepo.Object,
            _adminSettings.Object,
            _unitOfWork.Object
        );
    }

    [Theory]
    [InlineData(123456789, true)]
    [InlineData(987654321, true)]
    [InlineData(111111111, false)]
    public void IsAdmin_ShouldIdentifyAdminUsersCorrectly(long telegramId, bool expectedIsAdmin)
    {
        var result = _sut.IsAdmin(telegramId);
        result.Should().Be(expectedIsAdmin);
    }

    [Fact]
    public async Task GetOverallStatsAsync_ShouldDelegateToRepository()
    {
        var expectedStats = new AdminStatsDto(
            TotalUsers: 1000,
            CompletedProfiles: 800,
            BannedUsers: 10,
            MaleCount: 500,
            FemaleCount: 300,
            NewUsersLast24Hours: 50,
            NewUsersLast7Days: 200,
            NewUsersLast30Days: 600,
            DatingTargetFriendsCount: 300,
            DatingTargetRelationshipCount: 400,
            DatingTargetAdultOnlyCount: 100,
            AgeUnder18Count: 50,
            Age18To24Count: 400,
            Age25To34Count: 250,
            Age35To44Count: 80,
            Age45PlusCount: 20,
            TopCities: [new AdminCityStatsDto("Москва", "Россия", 300, 250, 150, 100)],
            TopCountries: [new AdminCountryStatsDto("Россия", 600)]
        );

        _userProfileRepo.Setup(r => r.GetAdminStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        var result = await _sut.GetOverallStatsAsync();

        result.Should().BeEquivalentTo(expectedStats);
        result.TotalUsers.Should().Be(1000);
        result.CompletedProfiles.Should().Be(800);
    }

    [Fact]
    public async Task GetCityStatsAsync_ShouldReturnCorrectCityStats()
    {
        var cityStats = new AdminCityStatsDto("Киев", "Украина", 150, 120, 80, 40);
        _userProfileRepo.Setup(r => r.GetCityStatsAsync("Киев", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cityStats);

        var result = await _sut.GetCityStatsAsync("Киев");

        result.Should().NotBeNull();
        result!.CityName.Should().Be("Киев");
        result.UserCount.Should().Be(150);
        result.MaleCount.Should().Be(80);
        result.FemaleCount.Should().Be(40);
    }

    [Fact]
    public async Task GetBroadcastAudienceCountAsync_ShouldReturnCountOfFilteredRecipients()
    {
        var filter = new AdminBroadcastFilterDto(TargetGender: Gender.Female, City: "Москва");
        _userProfileRepo.Setup(r => r.GetBroadcastRecipientsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync([111, 222, 333]);

        var count = await _sut.GetBroadcastAudienceCountAsync(filter);

        count.Should().Be(3);
    }

    [Fact]
    public async Task BanUserDirectlyAsync_ShouldSetBannedStateAndUncompleteProfile()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TelegramId = 555,
            Language = AppLanguage.Ukrainian,
            State = UserState.Active,
            FirstName = "Олег"
        };
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Олег",
            IsCompleted = true
        };

        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userProfileRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _sut.BanUserDirectlyAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TelegramId.Should().Be(555);
        result.Value.Language.Should().Be(AppLanguage.Ukrainian);
        user.State.Should().Be(UserState.Banned);
        profile.IsCompleted.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserProfileDirectlyAsync_ShouldResetProfileAndState()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TelegramId = 777,
            Language = AppLanguage.Russian,
            State = UserState.Active,
            FirstName = "Анна"
        };
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Анна",
            Age = 22,
            City = "Москва",
            IsCompleted = true
        };

        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userProfileRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _sut.DeleteUserProfileDirectlyAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TelegramId.Should().Be(777);
        user.State.Should().Be(UserState.Registration_SelectingLanguage);
        profile.Name.Should().BeNull();
        profile.Age.Should().BeNull();
        profile.City.Should().BeNull();
        profile.IsCompleted.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAdminProfileByGenderAsync_ShouldSupportEndlessPaginationWrapAround()
    {
        var user = new User { TelegramId = 999, State = UserState.Active };
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            User = user,
            Name = "Дмитрий",
            Age = 28,
            Gender = Gender.Male,
            IsCompleted = true
        };

        _userProfileRepo.Setup(r => r.GetAdminProfileByGenderAsync(Gender.Male, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((profile, 3));
        _interestRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var (dto, totalCount, curIdx) = await _sut.GetAdminProfileByGenderAsync(Gender.Male, 5);

        dto.Should().NotBeNull();
        dto!.Name.Should().Be("Дмитрий");
        totalCount.Should().Be(3);
        curIdx.Should().Be(3); // (5 % 3) + 1 = 3
    }

    [Fact]
    public async Task DeleteUserProfileDirectlyAsync_WhenPassedProfileId_ShouldLookupByProfileIdAndResetProfileAndState()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TelegramId = 777,
            Language = AppLanguage.Russian,
            State = UserState.Active,
            FirstName = "Анна"
        };
        var profile = new UserProfile
        {
            Id = profileId,
            UserId = userId,
            User = user,
            Name = "Анна",
            Age = 22,
            City = "Москва",
            IsCompleted = true
        };

        _userRepo.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _userProfileRepo.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _sut.DeleteUserProfileDirectlyAsync(profileId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TelegramId.Should().Be(777);
        user.State.Should().Be(UserState.Registration_SelectingLanguage);
        profile.Name.Should().BeNull();
        profile.Age.Should().BeNull();
        profile.City.Should().BeNull();
        profile.IsCompleted.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BanUserDirectlyAsync_WhenPassedProfileId_ShouldLookupByProfileIdAndSetBannedState()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TelegramId = 555,
            Language = AppLanguage.Ukrainian,
            State = UserState.Active,
            FirstName = "Олег"
        };
        var profile = new UserProfile
        {
            Id = profileId,
            UserId = userId,
            User = user,
            Name = "Олег",
            IsCompleted = true
        };

        _userRepo.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _userProfileRepo.Setup(r => r.GetByIdAsync(profileId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _sut.BanUserDirectlyAsync(profileId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TelegramId.Should().Be(555);
        result.Value.Language.Should().Be(AppLanguage.Ukrainian);
        user.State.Should().Be(UserState.Banned);
        profile.IsCompleted.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
