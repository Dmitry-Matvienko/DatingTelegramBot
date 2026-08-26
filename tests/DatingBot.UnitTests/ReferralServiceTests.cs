using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using User = DatingBot.Domain.Entities.User;

namespace DatingBot.UnitTests;

public class ReferralServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IReferralRepository> _referralRepository = new();
    private readonly Mock<IBotInfoProvider> _botInfoProvider = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly ReferralService _service;

    public ReferralServiceTests()
    {
        _botInfoProvider.Setup(b => b.GetBotUsernameAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("DatingBot");

        _service = new ReferralService(
            _userRepository.Object,
            _userProfileRepository.Object,
            _referralRepository.Object,
            _botInfoProvider.Object,
            _unitOfWork.Object
        );
    }

    [Fact]
    public async Task GetUserReferralLinkAsync_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        _userRepository.Setup(r => r.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.GetUserReferralLinkAsync(12345);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserReferralLinkAsync_WhenNoLinkExists_ShouldReturnSuccessWithNull()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), TelegramId = 12345 };
        _userRepository.Setup(r => r.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _referralRepository.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReferralLink?)null);

        // Act
        var result = await _service.GetUserReferralLinkAsync(12345);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetUserReferralLinkAsync_WhenLinkExists_ShouldReturnLinkDtoWithUrlAndRemainingBoostDays()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), TelegramId = 12345 };
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id, TopBoostUntil = DateTime.UtcNow.AddDays(4.5) };
        var link = new ReferralLink
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = "ref_abc12345",
            InvitedCount = 5,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        _userRepository.Setup(r => r.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _referralRepository.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        _userProfileRepository.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.GetUserReferralLinkAsync(12345);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Code.Should().Be("ref_abc12345");
        result.Value.LinkUrl.Should().Be("https://t.me/DatingBot?start=ref_abc12345");
        result.Value.InvitedCount.Should().Be(5);
        result.Value.RemainingBoostDays.Should().Be(5);
    }

    [Fact]
    public async Task CreateOrGetReferralLinkAsync_WhenNoLinkExists_ShouldCreateNewLinkAndReturnDto()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), TelegramId = 12345 };
        _userRepository.Setup(r => r.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _referralRepository.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReferralLink?)null);
        _referralRepository.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReferralLink?)null);

        // Act
        var result = await _service.CreateOrGetReferralLinkAsync(12345);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Code.Should().StartWith("ref_");
        result.Value.LinkUrl.Should().Be($"https://t.me/DatingBot?start={result.Value.Code}");
        result.Value.InvitedCount.Should().Be(0);

        _referralRepository.Verify(r => r.AddLinkAsync(It.Is<ReferralLink>(l => l.UserId == user.Id), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrGetReferralLinkAsync_WhenLinkAlreadyExists_ShouldReturnExistingLinkWithoutCreatingNew()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), TelegramId = 12345 };
        var existingLink = new ReferralLink
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = "ref_existing1",
            InvitedCount = 3,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        _userRepository.Setup(r => r.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _referralRepository.Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLink);

        // Act
        var result = await _service.CreateOrGetReferralLinkAsync(12345);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Code.Should().Be("ref_existing1");
        result.Value.InvitedCount.Should().Be(3);

        _referralRepository.Verify(r => r.AddLinkAsync(It.IsAny<ReferralLink>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessReferralJoinAsync_WhenCodeIsEmpty_ShouldReturnNull()
    {
        // Act
        var result = await _service.ProcessReferralJoinAsync(12345, "");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task ProcessReferralJoinAsync_WhenNewUserNotFound_ShouldReturnNull()
    {
        // Arrange
        _userRepository.Setup(r => r.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.ProcessReferralJoinAsync(12345, "ref_code123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task ProcessReferralJoinAsync_WhenUserAlreadyReferred_ShouldReturnNull()
    {
        // Arrange
        var newUser = new User { Id = Guid.NewGuid(), TelegramId = 12345, ReferredByUserId = Guid.NewGuid() };
        _userRepository.Setup(r => r.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);

        // Act
        var result = await _service.ProcessReferralJoinAsync(12345, "ref_code123");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        _referralRepository.Verify(r => r.AddRecordAsync(It.IsAny<ReferralRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessReferralJoinAsync_WhenSelfReferral_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, TelegramId = 12345 };
        var link = new ReferralLink { Id = Guid.NewGuid(), UserId = userId, Code = "ref_myself" };

        _userRepository.Setup(r => r.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _referralRepository.Setup(r => r.HasBeenReferredAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _referralRepository.Setup(r => r.GetByCodeAsync("ref_myself", It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);

        // Act
        var result = await _service.ProcessReferralJoinAsync(12345, "ref_myself");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        _referralRepository.Verify(r => r.AddRecordAsync(It.IsAny<ReferralRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessReferralJoinAsync_WhenValidNewUser_ShouldIncrementInvitedCount_AddRecord_ExtendTopBoostBy3Days_AndReturnDto()
    {
        // Arrange
        var referrerId = Guid.NewGuid();
        var referrerUser = new User { Id = referrerId, TelegramId = 99999, Language = AppLanguage.Russian };
        var referrerProfile = new UserProfile { Id = Guid.NewGuid(), UserId = referrerId, TopBoostUntil = null };
        var referralLink = new ReferralLink { Id = Guid.NewGuid(), UserId = referrerId, Code = "ref_valid1", InvitedCount = 2 };

        var newUserId = Guid.NewGuid();
        var newUser = new User { Id = newUserId, TelegramId = 11111 };

        _userRepository.Setup(r => r.GetByTelegramIdAsync(11111, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);
        _referralRepository.Setup(r => r.HasBeenReferredAsync(newUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _referralRepository.Setup(r => r.GetByCodeAsync("ref_valid1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(referralLink);
        _userRepository.Setup(r => r.GetByIdAsync(referrerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referrerUser);
        _userProfileRepository.Setup(r => r.GetByUserIdAsync(referrerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referrerProfile);

        // Act
        var result = await _service.ProcessReferralJoinAsync(11111, "ref_valid1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReferrerTelegramId.Should().Be(99999);
        result.Value.ReferrerLanguage.Should().Be(AppLanguage.Russian);
        result.Value.TotalBoostDays.Should().Be(3);

        referralLink.InvitedCount.Should().Be(3);
        newUser.ReferredByUserId.Should().Be(referrerId);
        referrerProfile.TopBoostUntil.Should().NotBeNull();
        referrerProfile.TopBoostUntil.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(3), TimeSpan.FromSeconds(5));

        _referralRepository.Verify(r => r.AddRecordAsync(It.Is<ReferralRecord>(rec => rec.ReferrerUserId == referrerId && rec.ReferredUserId == newUserId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessReferralJoinAsync_WhenReferrerAlreadyHasActiveBoost_ShouldAccumulate3Days_AndCalculateCorrectTotalDays()
    {
        // Arrange
        var referrerId = Guid.NewGuid();
        var referrerUser = new User { Id = referrerId, TelegramId = 99999, Language = AppLanguage.English };
        var existingBoost = DateTime.UtcNow.AddDays(4); // already 4 days active
        var referrerProfile = new UserProfile { Id = Guid.NewGuid(), UserId = referrerId, TopBoostUntil = existingBoost };
        var referralLink = new ReferralLink { Id = Guid.NewGuid(), UserId = referrerId, Code = "ref_accum", InvitedCount = 1 };

        var newUserId = Guid.NewGuid();
        var newUser = new User { Id = newUserId, TelegramId = 22222 };

        _userRepository.Setup(r => r.GetByTelegramIdAsync(22222, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newUser);
        _referralRepository.Setup(r => r.HasBeenReferredAsync(newUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _referralRepository.Setup(r => r.GetByCodeAsync("ref_accum", It.IsAny<CancellationToken>()))
            .ReturnsAsync(referralLink);
        _userRepository.Setup(r => r.GetByIdAsync(referrerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referrerUser);
        _userProfileRepository.Setup(r => r.GetByUserIdAsync(referrerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referrerProfile);

        // Act
        var result = await _service.ProcessReferralJoinAsync(22222, "ref_accum");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReferrerTelegramId.Should().Be(99999);
        result.Value.ReferrerLanguage.Should().Be(AppLanguage.English);
        result.Value.TotalBoostDays.Should().Be(7); // 4 + 3 = 7

        referrerProfile.TopBoostUntil.Value.Should().BeCloseTo(existingBoost.AddDays(3), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetTopReferrersAsync_ShouldReturnTopReferrersFromRepository()
    {
        // Arrange
        IReadOnlyList<ReferralTopUserDto> list =
        [
            new(Guid.NewGuid(), 111, "user1", "Alice", 10),
            new(Guid.NewGuid(), 222, null, "Bob", 5)
        ];

        _referralRepository.Setup(r => r.GetTopReferrersAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        // Act
        var result = await _service.GetTopReferrersAsync(15);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value![0].Name.Should().Be("Alice");
        result.Value[0].InvitedCount.Should().Be(10);
    }
}
