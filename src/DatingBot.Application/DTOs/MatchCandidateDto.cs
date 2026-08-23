using DatingBot.Domain.Enums;

namespace DatingBot.Application.DTOs;

public record MatchCandidateDto(
    UserProfileDto Profile,
    IReadOnlyList<InterestDto> CommonInterests,
    IReadOnlyList<InterestDto> OtherInterests,
    MatchTier Tier,
    string MatchReasonBadge,
    double? SimilarityScore = null,
    double? DistanceKm = null
);
