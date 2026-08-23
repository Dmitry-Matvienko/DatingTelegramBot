using DatingBot.Application.Interfaces;
using DatingBot.Application.Services;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DatingBot.UnitTests;

public class InactivityReminderServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly InactivityReminderService _service;

    public InactivityReminderServiceTests()
    {
        _service = new InactivityReminderService(_userRepositoryMock.Object);
    }

    [Fact]
    public void GetRandomInactivityReminderKey_ShouldReturnValidKeysFromOneToTen()
    {
        var validKeys = Enumerable.Range(1, 10)
            .Select(i => $"Notification_Inactivity_{i}")
            .ToHashSet();

        for (var i = 0; i < 100; i++)
        {
            var key = _service.GetRandomInactivityReminderKey();
            validKeys.Should().Contain(key);
        }
    }

    [Fact]
    public async Task GetUsersForInactivityReminderAsync_ShouldCallRepositoryWithCorrectCutoff()
    {
        var inactivityDays = 3;
        var limit = 50;
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), TelegramId = 111, State = UserState.Active }
        };

        _userRepositoryMock.Setup(r => r.GetInactiveUsersAsync(
            It.Is<DateTime>(d => (DateTime.UtcNow.AddDays(-inactivityDays) - d).TotalSeconds < 5),
            limit,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(users);

        var result = await _service.GetUsersForInactivityReminderAsync(inactivityDays, limit);

        result.Should().HaveCount(1);
        result[0].TelegramId.Should().Be(111);
        _userRepositoryMock.Verify(r => r.GetInactiveUsersAsync(It.IsAny<DateTime>(), limit, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkReminderSentAsync_ShouldDelegateToRepository()
    {
        var userId = Guid.NewGuid();
        var sentAt = DateTime.UtcNow;

        await _service.MarkReminderSentAsync(userId, sentAt);

        _userRepositoryMock.Verify(r => r.MarkInactivityReminderSentAsync(userId, sentAt, It.IsAny<CancellationToken>()), Times.Once);
    }
}
