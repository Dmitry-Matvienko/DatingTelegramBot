using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Interfaces;

public interface IAdminService
{
    bool IsAdmin(long telegramId);
    Task<AdminStatsDto> GetOverallStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminCityStatsDto>> GetTopCitiesStatsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<AdminCityStatsDto?> GetCityStatsAsync(string cityName, CancellationToken cancellationToken = default);
    Task<int> GetBroadcastAudienceCountAsync(AdminBroadcastFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<long>> GetBroadcastRecipientTelegramIdsAsync(AdminBroadcastFilterDto filter, CancellationToken cancellationToken = default);
    Task<int> GetPendingReportsCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminPendingReportDto>> GetPendingReportsAsync(int skip = 0, int take = 10, CancellationToken cancellationToken = default);
    Task<(UserProfileDto? Profile, int TotalCount, int CurrentIndex)> GetAdminProfileByGenderAsync(Gender gender, int offset, CancellationToken cancellationToken = default);
    Task<Result<AdminModerationActionResult>> BanUserDirectlyAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<AdminModerationActionResult>> DeleteUserProfileDirectlyAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AdminRevenueStatsDto> GetRevenueStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentTransactionDto>> GetRecentTransactionsAsync(int count = 20, CancellationToken cancellationToken = default);
    Task RecordSuccessfulPaymentAsync(long telegramId, int amount, string currency, PaymentType type, string payload, string? telegramPaymentChargeId, string? providerPaymentChargeId, CancellationToken cancellationToken = default);
}

