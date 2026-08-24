using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingBot.Infrastructure.Repositories;

public class ProfileRatingRepository(AppDbContext dbContext) : IProfileRatingRepository
{
    public async Task<bool> HasRatedAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProfileRatings
            .AnyAsync(r => r.FromUserId == fromUserId && r.ToUserId == toUserId, cancellationToken);
    }

    public async Task<ProfileRating?> GetRatingAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProfileRatings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.FromUserId == fromUserId && r.ToUserId == toUserId, cancellationToken);
    }

    public async Task<ProfileRating?> GetByIdWithProfilesAsync(Guid ratingId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProfileRatings
            .Include(r => r.FromUser)
            .Include(r => r.ToUser)
            .FirstOrDefaultAsync(r => r.Id == ratingId, cancellationToken);
    }

    public async Task<List<ProfileRating>> GetIncomingUnratedHighRatingsAsync(Guid toUserId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProfileRatings
            .AsNoTracking()
            .Include(r => r.FromUser)
            .Where(r => r.ToUserId == toUserId && r.Score >= 6)
            .Where(r => !dbContext.ProfileRatings.Any(back => back.FromUserId == toUserId && back.ToUserId == r.FromUserId))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<Guid>> GetRatedUserIdsAsync(Guid fromUserId, CancellationToken cancellationToken = default)
    {
        var ids = await dbContext.ProfileRatings
            .AsNoTracking()
            .Where(r => r.FromUserId == fromUserId)
            .Select(r => r.ToUserId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task AddAsync(ProfileRating rating, CancellationToken cancellationToken = default)
    {
        await dbContext.ProfileRatings.AddAsync(rating, cancellationToken);
    }
}
