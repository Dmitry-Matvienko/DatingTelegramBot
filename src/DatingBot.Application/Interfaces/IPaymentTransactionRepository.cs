using DatingBot.Application.DTOs;
using DatingBot.Domain.Entities;

namespace DatingBot.Application.Interfaces;

public interface IPaymentTransactionRepository
{
    Task AddAsync(PaymentTransaction transaction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentTransactionDto>> GetRecentAsync(int count = 20, CancellationToken cancellationToken = default);
    Task<AdminRevenueStatsDto> GetRevenueStatsAsync(CancellationToken cancellationToken = default);
}
