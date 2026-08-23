using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingBot.Infrastructure.Repositories;

public class PaymentTransactionRepository(AppDbContext dbContext) : IPaymentTransactionRepository
{
    public async Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default)
    {
        await dbContext.PaymentTransactions.AddAsync(transaction, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentTransactionDto>> GetRecentAsync(int count = 20, CancellationToken cancellationToken = default)
    {
        return await dbContext.PaymentTransactions
            .AsNoTracking()
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .Select(t => new PaymentTransactionDto(
                t.Id,
                t.TelegramId,
                t.User != null ? t.User.Username : null,
                t.User != null ? t.User.FirstName : null,
                t.Amount,
                t.Currency,
                t.Type,
                t.Payload,
                t.TelegramPaymentChargeId,
                t.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminRevenueStatsDto> GetRevenueStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalEarned = await dbContext.PaymentTransactions
            .SumAsync(t => (int?)t.Amount, cancellationToken) ?? 0;

        var totalCount = await dbContext.PaymentTransactions
            .CountAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var last24hCutoff = now.AddHours(-24);
        var last7dCutoff = now.AddDays(-7);
        var last30dCutoff = now.AddDays(-30);

        var earned24h = await dbContext.PaymentTransactions
            .Where(t => t.CreatedAt >= last24hCutoff)
            .SumAsync(t => (int?)t.Amount, cancellationToken) ?? 0;

        var earned7d = await dbContext.PaymentTransactions
            .Where(t => t.CreatedAt >= last7dCutoff)
            .SumAsync(t => (int?)t.Amount, cancellationToken) ?? 0;

        var earned30d = await dbContext.PaymentTransactions
            .Where(t => t.CreatedAt >= last30dCutoff)
            .SumAsync(t => (int?)t.Amount, cancellationToken) ?? 0;

        var recent = await GetRecentAsync(20, cancellationToken);

        return new AdminRevenueStatsDto(
            totalEarned,
            totalCount,
            earned24h,
            earned7d,
            earned30d,
            recent
        );
    }
}
