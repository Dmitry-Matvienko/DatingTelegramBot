using DatingBot.Domain.Entities;

namespace DatingBot.Application.Interfaces;

public interface IProfileRatingRepository
{
    Task<bool> HasRatedAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken = default);
    Task<ProfileRating?> GetRatingAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken = default);
    Task<ProfileRating?> GetByIdWithProfilesAsync(Guid ratingId, CancellationToken cancellationToken = default);
    Task<List<ProfileRating>> GetIncomingUnratedHighRatingsAsync(Guid toUserId, CancellationToken cancellationToken = default);
    Task<HashSet<Guid>> GetRatedUserIdsAsync(Guid fromUserId, CancellationToken cancellationToken = default);
    Task AddAsync(ProfileRating rating, CancellationToken cancellationToken = default);
}
