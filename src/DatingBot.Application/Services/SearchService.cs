using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Services;

public class SearchService(
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IInterestRepository interestRepository,
    IProfileRatingRepository profileRatingRepository,
    IProfileReportRepository profileReportRepository,
    IMatchmakingService matchmakingService,
    IUnitOfWork unitOfWork) : ISearchService
{
    public async Task<MatchCandidateDto?> GetNextMatchCandidateAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await matchmakingService.GetNextMatchCandidateAsync(telegramId, cancellationToken);
    }

    public async Task<UserProfileDto?> GetNextCandidateAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var match = await matchmakingService.GetNextMatchCandidateAsync(telegramId, cancellationToken);
        return match?.Profile;
    }

    public async Task<IncomingRatingDto?> GetNextIncomingRatingAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return null;

        var incomingRatings = await profileRatingRepository.GetIncomingUnratedHighRatingsAsync(user.Id, cancellationToken);
        if (incomingRatings.Count == 0) return null;

        var nextRating = incomingRatings[0];
        var raterProfile = await userProfileRepository.GetWithInterestsByUserIdAsync(nextRating.FromUserId, cancellationToken);
        if (raterProfile is null) return null;

        user.State = UserState.Searching_ViewingIncoming;
        user.CurrentCandidateProfileId = raterProfile.Id;
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var raterDto = await MapToDtoAsync(raterProfile, cancellationToken);
        return new IncomingRatingDto(nextRating.Id, raterDto, nextRating.Score, nextRating.CreatedAt);
    }

    public async Task<IncomingRatingDto?> GetIncomingRatingByIdAsync(long telegramId, Guid ratingId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return null;

        var rating = await profileRatingRepository.GetByIdWithProfilesAsync(ratingId, cancellationToken);
        if (rating is null || rating.ToUserId != user.Id) return null;

        var raterProfile = await userProfileRepository.GetWithInterestsByUserIdAsync(rating.FromUserId, cancellationToken);
        if (raterProfile is null) return null;

        user.State = UserState.Searching_ViewingIncoming;
        user.CurrentCandidateProfileId = raterProfile.Id;
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var raterDto = await MapToDtoAsync(raterProfile, cancellationToken);
        return new IncomingRatingDto(rating.Id, raterDto, rating.Score, rating.CreatedAt);
    }

    public async Task<Result<RatingResult>> RateCandidateAsync(long raterTelegramId, Guid candidateProfileId, int score, CancellationToken cancellationToken = default)
    {
        if (score < 1 || score > 10)
        {
            return Result<RatingResult>.Failure("Оценка должна быть в диапазоне от 1 до 10.");
        }

        var rater = await userRepository.GetByTelegramIdAsync(raterTelegramId, cancellationToken);
        if (rater is null) return Result<RatingResult>.Failure("Пользователь не найден.");

        var candidate = await userProfileRepository.GetByIdAsync(candidateProfileId, cancellationToken);
        if (candidate is null) return Result<RatingResult>.Failure("Анкета не найдена.");

        if (rater.Id == candidate.UserId)
        {
            return Result<RatingResult>.Failure("Нельзя оценить самого себя.");
        }

        var existingRating = await profileRatingRepository.GetRatingAsync(rater.Id, candidate.UserId, cancellationToken);
        Guid ratingId;
        int newCount;
        double newAverage;

        if (existingRating is not null)
        {
            ratingId = existingRating.Id;
            var oldScore = existingRating.Score;
            existingRating.Score = score;
            existingRating.CreatedAt = DateTime.UtcNow;
            profileRatingRepository.Update(existingRating);

            newCount = candidate.RatingCount > 0 ? candidate.RatingCount : 1;
            newAverage = Math.Round(((candidate.AverageRating * newCount) - oldScore + score) / newCount, 1);
            candidate.AverageRating = newAverage;
            candidate.RatingCount = newCount;
            candidate.UpdatedAt = DateTime.UtcNow;
            userProfileRepository.Update(candidate);
        }
        else
        {
            var rating = new ProfileRating
            {
                Id = Guid.NewGuid(),
                FromUserId = rater.Id,
                ToUserId = candidate.UserId,
                Score = score,
                CreatedAt = DateTime.UtcNow
            };

            await profileRatingRepository.AddAsync(rating, cancellationToken);
            ratingId = rating.Id;

            newCount = candidate.RatingCount + 1;
            newAverage = Math.Round(((candidate.AverageRating * candidate.RatingCount) + score) / newCount, 1);

            candidate.RatingCount = newCount;
            candidate.AverageRating = newAverage;
            candidate.UpdatedAt = DateTime.UtcNow;
            userProfileRepository.Update(candidate);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Проверка взаимной симпатии (оценил ли кандидат ранее оценивающего на 6+ баллов)
        var isMutualMatch = false;
        var originalScore = 0;
        var previousRating = await profileRatingRepository.GetRatingAsync(candidate.UserId, rater.Id, cancellationToken);
        if (previousRating is not null && previousRating.Score >= 6 && score >= 6)
        {
            isMutualMatch = true;
            originalScore = previousRating.Score;
        }

        var raterProfile = await userProfileRepository.GetWithInterestsByUserIdAsync(rater.Id, cancellationToken);
        var raterDto = raterProfile is not null ? await MapToDtoAsync(raterProfile, cancellationToken) : null;
        var candidateDto = await MapToDtoAsync(candidate, cancellationToken);

        return Result<RatingResult>.Success(new RatingResult(
            ratingId,
            candidate.User.TelegramId,
            score,
            newCount,
            newAverage,
            isMutualMatch,
            originalScore,
            raterDto,
            candidateDto
        ));
    }

    public async Task<Result<ReportInfo>> ReportCandidateAsync(long reporterTelegramId, Guid candidateProfileId, ReportReason reason, string? details = null, CancellationToken cancellationToken = default)
    {
        var reporter = await userRepository.GetByTelegramIdAsync(reporterTelegramId, cancellationToken);
        if (reporter is null) return Result<ReportInfo>.Failure("Пользователь не найден.");

        var candidate = await userProfileRepository.GetByIdAsync(candidateProfileId, cancellationToken);
        if (candidate is null) return Result<ReportInfo>.Failure("Анкета не найдена.");

        var report = new ProfileReport
        {
            Id = Guid.NewGuid(),
            ReporterId = reporter.Id,
            ReportedUserId = candidate.UserId,
            Reason = reason,
            Details = details?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await profileReportRepository.AddAsync(report, cancellationToken);

        reporter.State = UserState.Searching;
        reporter.CurrentCandidateProfileId = null;
        userRepository.Update(reporter);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var reportedDto = await MapToDtoAsync(candidate, cancellationToken);

        return Result<ReportInfo>.Success(new ReportInfo(
            report.Id,
            reportedDto,
            reporter.TelegramId,
            reporter.Username,
            reporter.FirstName,
            reason,
            details?.Trim()
        ));
    }

    public async Task<Result> SetReportingStateAsync(long telegramId, Guid candidateProfileId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure("Пользователь не найден.");

        user.State = UserState.Reporting_WaitingForDetails;
        user.CurrentCandidateProfileId = candidateProfileId;
        user.UpdatedAt = DateTime.UtcNow;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ClearCurrentCandidateAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure("Пользователь не найден.");

        user.CurrentCandidateProfileId = null;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ResetHistoryForCityAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await matchmakingService.ResetHistoryForCityAsync(telegramId, cancellationToken);
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
}
