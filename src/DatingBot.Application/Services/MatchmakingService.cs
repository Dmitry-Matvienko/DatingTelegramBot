using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Services;

public class MatchmakingService(
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IInterestRepository interestRepository,
    IProfileRatingRepository profileRatingRepository,
    IProfileReportRepository profileReportRepository,
    IAiEmbeddingService aiEmbeddingService,
    ILocalizationService loc,
    IUnitOfWork unitOfWork) : IMatchmakingService
{
    private const double AiSimilarityThreshold = 0.55;
    private const double MaxNearbyRadiusKm = 500.0;

    public async Task<MatchCandidateDto?> GetNextMatchCandidateAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return null;

        var lang = user.Language;

        var profile = await userProfileRepository.GetWithInterestsByUserIdAsync(user.Id, cancellationToken);
        if (profile is null || !profile.IsCompleted) return null;

        if (user.State != UserState.Searching)
        {
            user.State = UserState.Searching;
            user.UpdatedAt = DateTime.UtcNow;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var ratedIds = await profileRatingRepository.GetRatedUserIdsAsync(user.Id, cancellationToken);
        var reportedIds = await profileReportRepository.GetReportedUserIdsAsync(user.Id, cancellationToken);

        var excludedUserIds = new HashSet<Guid>(ratedIds);
        excludedUserIds.UnionWith(reportedIds);
        excludedUserIds.Add(user.Id);

        var candidates = await userProfileRepository.GetEligibleCandidatesAsync(profile, excludedUserIds, cancellationToken);
        if (candidates.Count == 0)
        {
            user.CurrentCandidateProfileId = null;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        var userInterests = profile.Interests.Select(i => i.InterestId).ToHashSet();
        float[]? userVector = profile.AiVector != null ? aiEmbeddingService.BytesToVector(profile.AiVector) : null;

        var evaluatedCandidates = new List<CandidateEvaluation>();

        foreach (var candidate in candidates)
        {
            var isSameCity = (profile.CityId.HasValue && candidate.CityId.HasValue && profile.CityId == candidate.CityId) ||
                             (!string.IsNullOrEmpty(profile.City) && !string.IsNullOrEmpty(candidate.City) &&
                              string.Equals(profile.City.Trim(), candidate.City.Trim(), StringComparison.OrdinalIgnoreCase));

            double? distanceKm = null;
            if (!isSameCity && profile.CityRef is not null && candidate.CityRef is not null)
            {
                distanceKm = GeoUtils.CalculateDistanceKm(
                    profile.CityRef.Latitude, profile.CityRef.Longitude,
                    candidate.CityRef.Latitude, candidate.CityRef.Longitude
                );
            }

            if (!isSameCity)
            {
                // Если кандидат из другого города, он должен быть строго в радиусе до 500 км
                if (!distanceKm.HasValue || distanceKm.Value > MaxNearbyRadiusKm)
                {
                    continue;
                }
            }

            var common = candidate.Interests.Where(i => userInterests.Contains(i.InterestId)).Select(i => i.Interest).ToList();
            var other = candidate.Interests.Where(i => !userInterests.Contains(i.InterestId)).Select(i => i.Interest).ToList();

            double similarity = 0.0;
            if (userVector is not null && candidate.AiVector is not null)
            {
                var candidateVector = aiEmbeddingService.BytesToVector(candidate.AiVector);
                similarity = aiEmbeddingService.CalculateCosineSimilarity(userVector, candidateVector);
            }

            // Классификация по уровням подбора
            MatchTier tier;
            string badge;

            if (isSameCity)
            {
                if (similarity >= AiSimilarityThreshold)
                {
                    tier = MatchTier.AiCompatibility;
                    badge = loc.Get(lang, "Badge_Ai");
                }
                else if (common.Count > 0)
                {
                    tier = MatchTier.CommonInterests;
                    badge = loc.FormatCommonInterestsBadge(lang, common.Count);
                }
                else
                {
                    tier = MatchTier.SameCity;
                    badge = loc.Get(lang, "Badge_SameCity");
                }
            }
            else
            {
                tier = MatchTier.NearbyCity;
                var cityName = candidate.City ?? "-";
                if (distanceKm.HasValue)
                {
                    badge = loc.Get(lang, "Badge_NearbyCityDistance", cityName, Math.Round(distanceKm.Value));
                }
                else
                {
                    badge = loc.Get(lang, "Badge_NearbyCity", cityName);
                }
            }

            evaluatedCandidates.Add(new CandidateEvaluation(candidate, common, other, tier, badge, similarity, distanceKm));
        }

        if (evaluatedCandidates.Count == 0)
        {
            user.CurrentCandidateProfileId = null;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }

        // Многоуровневая сортировка (Каскад приоритетов):
        // 1. Tier 1 (ИИ-мэтч в городе) -> сортировка по сходству DESC
        // 2. Tier 2 (Общие интересы в городе) -> сортировка по кол-ву общих интересов DESC
        // 3. Tier 3 (Свой город) -> по дате обновления DESC
        // 4. Tier 4 (Соседние города) -> по расстоянию ASC, затем по сходству/интересам
        var best = evaluatedCandidates
            .OrderBy(c => c.Tier switch
            {
                MatchTier.AiCompatibility => 1,
                MatchTier.CommonInterests => 2,
                MatchTier.SameCity => 3,
                MatchTier.NearbyCity => 4,
                _ => 5
            })
            .ThenBy(c => c.Tier == MatchTier.NearbyCity ? (c.DistanceKm ?? 99999) : 0)
            .ThenByDescending(c => c.Tier == MatchTier.AiCompatibility ? c.Similarity : 0)
            .ThenByDescending(c => c.Tier == MatchTier.CommonInterests ? c.CommonInterests.Count : 0)
            .ThenByDescending(c => c.Candidate.UpdatedAt ?? c.Candidate.CreatedAt)
            .First();

        user.CurrentCandidateProfileId = best.Candidate.Id;
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var profileDto = await MapToDtoAsync(best.Candidate, cancellationToken);
        var commonDtos = best.CommonInterests.Select(i => new InterestDto(i.Id, i.Code, i.Title, i.Icon, true)).ToList();
        var otherDtos = best.OtherInterests.Select(i => new InterestDto(i.Id, i.Code, i.Title, i.Icon, true)).ToList();

        return new MatchCandidateDto(
            profileDto,
            commonDtos,
            otherDtos,
            best.Tier,
            best.Badge,
            best.Similarity,
            best.DistanceKm
        );
    }

    public async Task<Result> ResetHistoryForCityAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        await userProfileRepository.ResetRatingsForCityAsync(user.Id, profile.CityId, profile.City, cancellationToken);
        return Result.Success();
    }

    private async Task<UserProfileDto> MapToDtoAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        var allInterests = (await interestRepository.GetAllAsync(cancellationToken)) ?? [];
        var selectedInterestIds = profile.Interests?.Select(i => i.InterestId).ToHashSet() ?? [];

        var interestDtos = allInterests
            .Where(i => selectedInterestIds.Contains(i.Id))
            .Select(i => new InterestDto(i.Id, i.Code, i.Title, i.Icon, true))
            .ToList();

        return new UserProfileDto(
            profile.Id,
            profile.User?.TelegramId ?? 0,
            profile.User?.Username,
            profile.Gender,
            profile.TargetGender,
            profile.Name,
            profile.Age,
            profile.City,
            profile.Height,
            profile.PhotoFileId,
            profile.DatingTarget,
            profile.AiDescription,
            interestDtos,
            profile.IsCompleted,
            profile.AgeFilters,
            profile.SearchMinAge,
            profile.SearchMaxAge,
            profile.RatingCount,
            profile.AverageRating,
            profile.CityId,
            profile.AiVector,
            profile.Greeting
        );
    }

    private record CandidateEvaluation(
        UserProfile Candidate,
        List<Interest> CommonInterests,
        List<Interest> OtherInterests,
        MatchTier Tier,
        string Badge,
        double Similarity,
        double? DistanceKm
    );
}
