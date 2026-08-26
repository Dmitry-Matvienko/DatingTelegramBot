using DatingBot.Application.Common;
using DatingBot.Application.DTOs;

namespace DatingBot.Application.Interfaces;

public interface IReferralService
{
    Task<Result<ReferralLinkDto?>> GetUserReferralLinkAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result<ReferralLinkDto>> CreateOrGetReferralLinkAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result<ReferralProcessedDto?>> ProcessReferralJoinAsync(long newTelegramId, string referralCode, CancellationToken cancellationToken = default);
}
