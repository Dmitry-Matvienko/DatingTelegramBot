using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using DatingBot.Infrastructure.Data;
using DatingBot.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DatingBot.UnitTests;

public class UserRepositoryInactivityTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"UserInactivityDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetInactiveUsersAsync_ShouldReturnOnlyEligibleInactiveUsers()
    {
        using var context = CreateInMemoryDbContext();
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-3);

        // 1. Подходит: неактивен 5 дней, анкета завершена, не забанен, напоминаний не было
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 101,
            State = UserState.Active,
            LastActiveAt = now.AddDays(-5),
            LastInactivityReminderSentAt = null
        };
        var profile1 = new UserProfile { Id = Guid.NewGuid(), UserId = user1.Id, IsCompleted = true };
        context.Users.Add(user1);
        context.UserProfiles.Add(profile1);

        // 2. Не подходит: активен 1 день назад (свежий)
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 102,
            State = UserState.Active,
            LastActiveAt = now.AddDays(-1),
            LastInactivityReminderSentAt = null
        };
        var profile2 = new UserProfile { Id = Guid.NewGuid(), UserId = user2.Id, IsCompleted = true };
        context.Users.Add(user2);
        context.UserProfiles.Add(profile2);

        // 3. Не подходит: неактивен 5 дней, но анкета НЕ завершена
        var user3 = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 103,
            State = UserState.Registration_WaitingForPhoto,
            LastActiveAt = now.AddDays(-5),
            LastInactivityReminderSentAt = null
        };
        var profile3 = new UserProfile { Id = Guid.NewGuid(), UserId = user3.Id, IsCompleted = false };
        context.Users.Add(user3);
        context.UserProfiles.Add(profile3);

        // 4. Не подходит: забанен
        var user4 = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 104,
            State = UserState.Banned,
            LastActiveAt = now.AddDays(-5),
            LastInactivityReminderSentAt = null
        };
        var profile4 = new UserProfile { Id = Guid.NewGuid(), UserId = user4.Id, IsCompleted = true };
        context.Users.Add(user4);
        context.UserProfiles.Add(profile4);

        // 5. Не подходит: неактивен 5 дней, но напоминание уже отправлено 1 день назад
        var user5 = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 105,
            State = UserState.Active,
            LastActiveAt = now.AddDays(-5),
            LastInactivityReminderSentAt = now.AddDays(-1)
        };
        var profile5 = new UserProfile { Id = Guid.NewGuid(), UserId = user5.Id, IsCompleted = true };
        context.Users.Add(user5);
        context.UserProfiles.Add(profile5);

        // 6. Подходит: неактивен 5 дней, напоминание было 4 дня назад (прошло > 3 дней)
        var user6 = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 106,
            State = UserState.Searching,
            LastActiveAt = now.AddDays(-5),
            LastInactivityReminderSentAt = now.AddDays(-4)
        };
        var profile6 = new UserProfile { Id = Guid.NewGuid(), UserId = user6.Id, IsCompleted = true };
        context.Users.Add(user6);
        context.UserProfiles.Add(profile6);

        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var inactive = await repo.GetInactiveUsersAsync(cutoff, limit: 10);

        inactive.Should().HaveCount(2);
        inactive.Select(u => u.TelegramId).Should().BeEquivalentTo([101, 106]);
    }

    [Fact]
    public async Task MarkInactivityReminderSentAsync_ShouldUpdateTimestamp()
    {
        using var context = CreateInMemoryDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 201,
            State = UserState.Active,
            LastActiveAt = DateTime.UtcNow.AddDays(-4),
            LastInactivityReminderSentAt = null
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var sentTime = DateTime.UtcNow;
        await repo.MarkInactivityReminderSentAsync(user.Id, sentTime);

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.LastInactivityReminderSentAt.Should().Be(sentTime);
    }

    [Fact]
    public async Task UpdateLastActiveAtAsync_ShouldUpdateUserLastActiveAt()
    {
        using var context = CreateInMemoryDbContext();
        var oldTime = DateTime.UtcNow.AddDays(-5);
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 301,
            State = UserState.Active,
            LastActiveAt = oldTime
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new UserRepository(context);
        var newTime = DateTime.UtcNow;
        await repo.UpdateLastActiveAtAsync(301, newTime);

        var updatedUser = await context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.LastActiveAt.Should().Be(newTime);
    }
}
