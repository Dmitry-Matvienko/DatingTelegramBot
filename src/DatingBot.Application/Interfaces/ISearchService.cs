using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Interfaces;

public record RatingResult(
    Guid RatingId,
    long ToTelegramId,
    int Score,
    int NewRatingCount,
    double NewAverageRating,
    bool IsMutualMatch,
    int OriginalScore,
    UserProfileDto? RaterProfile,
    UserProfileDto? CandidateProfile,
    bool WasRecentlyRated = false
);

public record ReportInfo(
    Guid ReportId,
    UserProfileDto ReportedProfile,
    long ReporterTelegramId,
    string? ReporterUsername,
    string? ReporterFirstName,
    ReportReason Reason,
    string? Details
);

public interface ISearchService
{
    Task<MatchCandidateDto?> GetNextMatchCandidateAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetNextCandidateAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<IncomingRatingDto?> GetNextIncomingRatingAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<IncomingRatingDto?> GetIncomingRatingByIdAsync(long telegramId, Guid ratingId, CancellationToken cancellationToken = default);
    Task<Result<RatingResult>> RateCandidateAsync(long raterTelegramId, Guid candidateProfileId, int score, CancellationToken cancellationToken = default);
    Task<Result<ReportInfo>> ReportCandidateAsync(long reporterTelegramId, Guid candidateProfileId, ReportReason reason, string? details = null, CancellationToken cancellationToken = default);
    Task<Result> SetReportingStateAsync(long telegramId, Guid candidateProfileId, CancellationToken cancellationToken = default);
    Task<Result> ClearCurrentCandidateAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result> ResetHistoryForCityAsync(long telegramId, CancellationToken cancellationToken = default);
}
