using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace DatingBot.UnitTests;

public class ModerationServiceTests
{
    private readonly Mock<IProfileReportRepository> _reportRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUserProfileRepository> _profileRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ModerationService _service;

    public ModerationServiceTests()
    {
        _service = new ModerationService(
            _reportRepoMock.Object,
            _userRepoMock.Object,
            _profileRepoMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task BanUserByReportAsync_ShouldSetUserStateBannedAndDeactivateProfile()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var reporter = new User { Id = Guid.NewGuid(), TelegramId = 11111, Language = AppLanguage.Ukrainian };
        var reportedUser = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 22222,
            Language = AppLanguage.Russian,
            State = UserState.Active,
            Profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                Name = "Нарушитель",
                IsCompleted = true
            }
        };

        var report = new ProfileReport
        {
            Id = reportId,
            ReporterId = reporter.Id,
            Reporter = reporter,
            ReportedUserId = reportedUser.Id,
            ReportedUser = reportedUser,
            Reason = ReportReason.InappropriateContent
        };

        _reportRepoMock.Setup(r => r.GetByIdWithUsersAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.BanUserByReportAsync(reportId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReportId.Should().Be(reportId);
        result.Value.ReporterTelegramId.Should().Be(11111);
        result.Value.ReporterLanguage.Should().Be(AppLanguage.Ukrainian);
        result.Value.ReportedTelegramId.Should().Be(22222);
        result.Value.ReportedLanguage.Should().Be(AppLanguage.Russian);
        result.Value.ShouldNotifyReporter.Should().BeTrue();

        reportedUser.State.Should().Be(UserState.Banned);
        reportedUser.Profile.IsCompleted.Should().BeFalse();

        _userRepoMock.Verify(u => u.Update(reportedUser), Times.Once);
        _profileRepoMock.Verify(p => p.Update(reportedUser.Profile), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BanUserByReportAsync_ShouldFail_WhenReportNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportRepoMock.Setup(r => r.GetByIdWithUsersAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfileReport?)null);

        // Act
        var result = await _service.BanUserByReportAsync(reportId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("не найдена");
    }

    [Fact]
    public async Task BanUserByReportAsync_ShouldFail_WhenUserAlreadyBanned()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var reporter = new User { Id = Guid.NewGuid(), TelegramId = 11111 };
        var reportedUser = new User { Id = Guid.NewGuid(), TelegramId = 22222, State = UserState.Banned };
        var report = new ProfileReport
        {
            Id = reportId,
            Reporter = reporter,
            ReportedUser = reportedUser
        };

        _reportRepoMock.Setup(r => r.GetByIdWithUsersAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.BanUserByReportAsync(reportId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("уже заблокирован");
    }

    [Fact]
    public async Task DeleteProfileByReportAsync_ShouldResetProfileAndState()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var reporter = new User { Id = Guid.NewGuid(), TelegramId = 11111, Language = AppLanguage.English };
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            Name = "Иван",
            Age = 25,
            City = "Киев",
            PhotoFileId = "photo_123",
            IsCompleted = true
        };
        var reportedUser = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 22222,
            Language = AppLanguage.Ukrainian,
            State = UserState.Active,
            Profile = profile
        };

        var report = new ProfileReport
        {
            Id = reportId,
            Reporter = reporter,
            ReportedUser = reportedUser
        };

        _reportRepoMock.Setup(r => r.GetByIdWithUsersAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.DeleteProfileByReportAsync(reportId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ReporterTelegramId.Should().Be(11111);
        result.Value.ReporterLanguage.Should().Be(AppLanguage.English);
        result.Value.ReportedTelegramId.Should().Be(22222);
        result.Value.ReportedLanguage.Should().Be(AppLanguage.Ukrainian);

        profile.IsCompleted.Should().BeFalse();
        profile.Name.Should().BeNull();
        profile.Age.Should().BeNull();
        profile.City.Should().BeNull();
        profile.PhotoFileId.Should().BeNull();

        reportedUser.State.Should().Be(UserState.Registration_SelectingLanguage);

        _profileRepoMock.Verify(p => p.SetInterestsAsync(profile.Id, It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        _profileRepoMock.Verify(p => p.Update(profile), Times.Once);
        _userRepoMock.Verify(u => u.Update(reportedUser), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProfileByReportAsync_ShouldFail_WhenReportNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportRepoMock.Setup(r => r.GetByIdWithUsersAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfileReport?)null);

        // Act
        var result = await _service.DeleteProfileByReportAsync(reportId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("не найдена");
    }

    [Fact]
    public async Task IgnoreReportAsync_ShouldSucceed_WhenReportExists()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = new ProfileReport { Id = reportId };
        _reportRepoMock.Setup(r => r.GetByIdWithUsersAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.IgnoreReportAsync(reportId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        report.IsResolved.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IgnoreReportAsync_ShouldFail_WhenReportNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportRepoMock.Setup(r => r.GetByIdWithUsersAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfileReport?)null);

        // Act
        var result = await _service.IgnoreReportAsync(reportId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("не найдена");
    }

    [Fact]
    public async Task UnbanUserAsync_ShouldSetActiveStateAndRestoreProfile_WhenProfileWasCompleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TelegramId = 55555,
            Language = AppLanguage.Russian,
            State = UserState.Banned
        };
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Алексей",
            Age = 25,
            Gender = Gender.Male,
            TargetGender = TargetGender.Female,
            CityId = 1,
            PhotoFileId = "photo_abc",
            IsCompleted = false
        };

        _userRepoMock.Setup(u => u.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(p => p.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _service.UnbanUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TelegramId.Should().Be(55555);
        result.Value.Language.Should().Be(AppLanguage.Russian);
        result.Value.HasCompletedProfile.Should().BeTrue();

        user.State.Should().Be(UserState.Active);
        profile.IsCompleted.Should().BeTrue();

        _profileRepoMock.Verify(p => p.Update(profile), Times.Once);
        _userRepoMock.Verify(u => u.Update(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnbanUserAsync_ShouldSetSelectingLanguageState_WhenProfileIsIncomplete()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TelegramId = 66666,
            Language = AppLanguage.English,
            State = UserState.Banned
        };

        _userRepoMock.Setup(u => u.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(p => p.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.UnbanUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TelegramId.Should().Be(66666);
        result.Value.Language.Should().Be(AppLanguage.English);
        result.Value.HasCompletedProfile.Should().BeFalse();

        user.State.Should().Be(UserState.Registration_SelectingLanguage);

        _userRepoMock.Verify(u => u.Update(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnbanUserAsync_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(u => u.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.UnbanUserAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("не найден");
    }

    [Fact]
    public async Task UnbanUserByTelegramIdAsync_ShouldSucceed_WhenUserExists()
    {
        // Arrange
        var telegramId = 77777L;
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = telegramId,
            Language = AppLanguage.Portuguese,
            State = UserState.Banned
        };

        _userRepoMock.Setup(u => u.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepoMock.Setup(p => p.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act
        var result = await _service.UnbanUserByTelegramIdAsync(telegramId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TelegramId.Should().Be(telegramId);
        result.Value.Language.Should().Be(AppLanguage.Portuguese);
    }
}
