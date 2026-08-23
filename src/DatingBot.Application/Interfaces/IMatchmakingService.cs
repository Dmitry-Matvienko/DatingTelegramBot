using DatingBot.Application.Common;
using DatingBot.Application.DTOs;

namespace DatingBot.Application.Interfaces;

public interface IMatchmakingService
{
    Task<MatchCandidateDto?> GetNextMatchCandidateAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result> ResetHistoryForCityAsync(long telegramId, CancellationToken cancellationToken = default);
}
