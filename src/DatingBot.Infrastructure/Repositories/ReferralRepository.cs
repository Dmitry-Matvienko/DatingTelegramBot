using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingBot.Infrastructure.Repositories;

public class ReferralRepository(AppDbContext dbContext) : IReferralRepository
{
    public async Task<ReferralLink?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ReferralLinks
            .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);
    }

    public async Task<ReferralLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await dbContext.ReferralLinks
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);
    }

    public async Task AddLinkAsync(ReferralLink referralLink, CancellationToken cancellationToken = default)
    {
        await dbContext.ReferralLinks.AddAsync(referralLink, cancellationToken);
    }

    public void UpdateLink(ReferralLink referralLink)
    {
        dbContext.ReferralLinks.Update(referralLink);
    }

    public async Task AddRecordAsync(ReferralRecord record, CancellationToken cancellationToken = default)
    {
        await dbContext.ReferralRecords.AddAsync(record, cancellationToken);
    }

    public async Task<bool> HasBeenReferredAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ReferralRecords
            .AnyAsync(r => r.ReferredUserId == userId, cancellationToken);
    }

    public async Task<int> GetInvitedCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ReferralRecords
            .CountAsync(r => r.ReferrerUserId == userId, cancellationToken);
    }
}
