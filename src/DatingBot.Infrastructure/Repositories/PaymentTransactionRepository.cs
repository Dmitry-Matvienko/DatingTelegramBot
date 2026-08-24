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
        var now = DateTime.UtcNow;
        var last24hCutoff = now.AddHours(-24);
        var last7dCutoff = now.AddDays(-7);
        var last30dCutoff = now.AddDays(-30);

        var stats = await dbContext.PaymentTransactions
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalEarned = g.Sum(t => (int?)t.Amount) ?? 0,
                TotalCount = g.Count(),
                Earned24h = g.Where(t => t.CreatedAt >= last24hCutoff).Sum(t => (int?)t.Amount) ?? 0,
                Earned7d = g.Where(t => t.CreatedAt >= last7dCutoff).Sum(t => (int?)t.Amount) ?? 0,
                Earned30d = g.Where(t => t.CreatedAt >= last30dCutoff).Sum(t => (int?)t.Amount) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        var recent = await GetRecentAsync(20, cancellationToken);

        return new AdminRevenueStatsDto(
            stats?.TotalEarned ?? 0,
            stats?.TotalCount ?? 0,
            stats?.Earned24h ?? 0,
            stats?.Earned7d ?? 0,
            stats?.Earned30d ?? 0,
            recent
        );
    }
}
