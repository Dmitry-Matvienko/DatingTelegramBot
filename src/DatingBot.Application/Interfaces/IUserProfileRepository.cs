using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetWithInterestsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetWithInterestsByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetNextCandidateForUserAsync(UserProfile currentUserProfile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserProfile>> GetEligibleCandidatesAsync(UserProfile currentUserProfile, int limit = 100, CancellationToken cancellationToken = default);
    Task<int> GetTotalEligibleCandidatesCountAsync(UserProfile currentUserProfile, CancellationToken cancellationToken = default);
    Task<int> ResetRatingsForCityAsync(Guid userId, int? cityId, string? cityName, CancellationToken cancellationToken = default);
    Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default);
    void Update(UserProfile profile);
    Task SetInterestsAsync(Guid userProfileId, IEnumerable<int> interestIds, CancellationToken cancellationToken = default);
    Task<(UserProfile? Profile, int TotalCount)> GetAdminProfileByGenderAsync(Gender gender, int offset, CancellationToken cancellationToken = default);
    Task<DTOs.AdminStatsDto> GetAdminStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DTOs.AdminCityStatsDto>> GetTopCitiesStatsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<DTOs.AdminCityStatsDto?> GetCityStatsAsync(string cityName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<long>> GetBroadcastRecipientsAsync(DTOs.AdminBroadcastFilterDto filter, CancellationToken cancellationToken = default);
}
