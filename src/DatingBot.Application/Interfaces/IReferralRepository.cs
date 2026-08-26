using DatingBot.Domain.Entities;

namespace DatingBot.Application.Interfaces;

public interface IReferralRepository
{
    Task<ReferralLink?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ReferralLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddLinkAsync(ReferralLink referralLink, CancellationToken cancellationToken = default);
    void UpdateLink(ReferralLink referralLink);
    Task AddRecordAsync(ReferralRecord record, CancellationToken cancellationToken = default);
    Task<bool> HasBeenReferredAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetInvitedCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
