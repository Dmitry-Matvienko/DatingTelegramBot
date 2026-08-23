using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using DatingBot.Infrastructure.Data;
using DatingBot.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DatingBot.UnitTests;

public class PaymentTransactionRepositoryTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"PaymentTxDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_And_GetRecentAsync_ShouldReturnSavedTransactions()
    {
        using var context = CreateInMemoryDbContext();
        var user = new User { Id = Guid.NewGuid(), TelegramId = 111222, Username = "payer", FirstName = "John", State = UserState.Active };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repo = new PaymentTransactionRepository(context);

        var tx = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TelegramId = user.TelegramId,
            Amount = 100,
            Currency = "XTR",
            Type = PaymentType.Unban,
            Payload = "unban:111",
            TelegramPaymentChargeId = "ch_123",
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(tx);
        await context.SaveChangesAsync();

        var recent = await repo.GetRecentAsync(20);
        recent.Should().HaveCount(1);
        recent[0].Amount.Should().Be(100);
        recent[0].TelegramId.Should().Be(111222);
        recent[0].Username.Should().Be("payer");
        recent[0].FirstName.Should().Be("John");
        recent[0].Type.Should().Be(PaymentType.Unban);
        recent[0].TelegramPaymentChargeId.Should().Be("ch_123");
    }

    [Fact]
    public async Task GetRevenueStatsAsync_ShouldCalculateMetricsCorrectly()
    {
        using var context = CreateInMemoryDbContext();
        var user = new User { Id = Guid.NewGuid(), TelegramId = 333444, Username = "payer2", FirstName = "Alice", State = UserState.Active };
        context.Users.Add(user);

        var now = DateTime.UtcNow;

        // Транзакция 2 часа назад (входит в 24ч, 7д, 30д)
        context.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TelegramId = 333444,
            Amount = 100,
            Type = PaymentType.Unban,
            Payload = "unban:1",
            CreatedAt = now.AddHours(-2)
        });

        // Транзакция 3 дня назад (входит в 7д, 30д, но не в 24ч)
        context.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TelegramId = 333444,
            Amount = 200,
            Type = PaymentType.Subscription,
            Payload = "sub:1",
            CreatedAt = now.AddDays(-3)
        });

        // Транзакция 15 дней назад (входит в 30д, но не в 7д и не в 24ч)
        context.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TelegramId = 333444,
            Amount = 300,
            Type = PaymentType.Other,
            Payload = "other:1",
            CreatedAt = now.AddDays(-15)
        });

        // Транзакция 40 дней назад (не входит в 24ч, 7д, 30д, но входит в общий баланс)
        context.PaymentTransactions.Add(new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TelegramId = 333444,
            Amount = 400,
            Type = PaymentType.Other,
            Payload = "old:1",
            CreatedAt = now.AddDays(-40)
        });

        await context.SaveChangesAsync();

        var repo = new PaymentTransactionRepository(context);
        var stats = await repo.GetRevenueStatsAsync();

        stats.TotalEarnedStars.Should().Be(1000);
        stats.TotalTransactionsCount.Should().Be(4);
        stats.EarnedLast24Hours.Should().Be(100);
        stats.EarnedLast7Days.Should().Be(300); // 100 + 200
        stats.EarnedLast30Days.Should().Be(600); // 100 + 200 + 300
        stats.RecentTransactions.Should().HaveCount(4);
        stats.RecentTransactions[0].Amount.Should().Be(100); // самая свежая
    }
}
