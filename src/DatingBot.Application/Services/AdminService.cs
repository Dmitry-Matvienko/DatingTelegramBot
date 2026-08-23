using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Services;

public class AdminService(
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IProfileReportRepository profileReportRepository,
    IInterestRepository interestRepository,
    IAdminSettings adminSettings,
    IUnitOfWork unitOfWork) : IAdminService
{
    public bool IsAdmin(long telegramId)
    {
        return adminSettings.AdminIds.Contains(telegramId);
    }

    public async Task<AdminStatsDto> GetOverallStatsAsync(CancellationToken cancellationToken = default)
    {
        return await userProfileRepository.GetAdminStatsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminCityStatsDto>> GetTopCitiesStatsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await userProfileRepository.GetTopCitiesStatsAsync(count, cancellationToken);
    }

    public async Task<AdminCityStatsDto?> GetCityStatsAsync(string cityName, CancellationToken cancellationToken = default)
    {
        return await userProfileRepository.GetCityStatsAsync(cityName, cancellationToken);
    }

    public async Task<int> GetBroadcastAudienceCountAsync(AdminBroadcastFilterDto filter, CancellationToken cancellationToken = default)
    {
        var recipients = await userProfileRepository.GetBroadcastRecipientsAsync(filter, cancellationToken);
        return recipients.Count;
    }

    public async Task<IReadOnlyList<long>> GetBroadcastRecipientTelegramIdsAsync(AdminBroadcastFilterDto filter, CancellationToken cancellationToken = default)
    {
        return await userProfileRepository.GetBroadcastRecipientsAsync(filter, cancellationToken);
    }

    public async Task<int> GetPendingReportsCountAsync(CancellationToken cancellationToken = default)
    {
        return await profileReportRepository.GetPendingReportsCountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminPendingReportDto>> GetPendingReportsAsync(int skip = 0, int take = 10, CancellationToken cancellationToken = default)
    {
        var reports = await profileReportRepository.GetPendingReportsAsync(skip, take, cancellationToken);
        var allInterests = await interestRepository.GetAllAsync(cancellationToken);

        var result = new List<AdminPendingReportDto>();

        foreach (var r in reports)
        {
            var reportedUser = r.ReportedUser;
            var reportedProfile = reportedUser.Profile;

            var interestDtos = new List<InterestDto>();
            if (reportedProfile is not null)
            {
                var selectedIds = reportedProfile.Interests.Select(i => i.InterestId).ToHashSet();
                interestDtos = allInterests
                    .Where(i => selectedIds.Contains(i.Id))
                    .Select(i => new InterestDto(i.Id, i.Code, i.Title, i.Icon, true))
                    .ToList();
            }

            var profileDto = new UserProfileDto(
                reportedProfile?.Id ?? Guid.Empty,
                reportedUser.TelegramId,
                reportedUser.Username,
                reportedProfile?.Gender,
                reportedProfile?.TargetGender,
                reportedProfile?.Name ?? reportedUser.FirstName,
                reportedProfile?.Age,
                reportedProfile?.City,
                reportedProfile?.Height,
                reportedProfile?.PhotoFileId,
                reportedProfile?.DatingTarget,
                reportedProfile?.AiDescription,
                interestDtos,
                reportedProfile?.IsCompleted ?? false,
                reportedProfile?.AgeFilters ?? AgeCategoryFilter.None,
                reportedProfile?.SearchMinAge,
                reportedProfile?.SearchMaxAge,
                reportedProfile?.RatingCount ?? 0,
                reportedProfile?.AverageRating ?? 0,
                reportedProfile?.CityId,
                reportedProfile?.AiVector,
                reportedProfile?.Greeting
            );

            result.Add(new AdminPendingReportDto(
                r.Id,
                r.ReporterId,
                r.Reporter.TelegramId,
                r.Reporter.Username,
                r.Reporter.FirstName,
                r.Reporter.Language,
                r.ReportedUserId,
                profileDto,
                r.Reason,
                r.Details,
                r.CreatedAt
            ));
        }

        return result;
    }

    public async Task<(UserProfileDto? Profile, int TotalCount, int CurrentIndex)> GetAdminProfileByGenderAsync(Gender gender, int offset, CancellationToken cancellationToken = default)
    {
        var (profile, totalCount) = await userProfileRepository.GetAdminProfileByGenderAsync(gender, offset, cancellationToken);
        if (profile is null || totalCount == 0)
        {
            return (null, 0, 0);
        }

        var allInterests = await interestRepository.GetAllAsync(cancellationToken);
        var selectedInterestIds = profile.Interests.Select(i => i.InterestId).ToHashSet();

        var interestDtos = allInterests
            .Where(i => selectedInterestIds.Contains(i.Id))
            .Select(i => new InterestDto(i.Id, i.Code, i.Title, i.Icon, true))
            .ToList();

        var profileDto = new UserProfileDto(
            profile.Id,
            profile.User.TelegramId,
            profile.User.Username,
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

        var currentIndex = (offset % totalCount) + 1;
        return (profileDto, totalCount, currentIndex);
    }

    public async Task<Result<AdminModerationActionResult>> BanUserDirectlyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<AdminModerationActionResult>.Failure("Пользователь не найден.");
        }

        user.State = UserState.Banned;
        user.CurrentCandidateProfileId = null;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is not null)
        {
            profile.IsCompleted = false;
            profile.UpdatedAt = DateTime.UtcNow;
            userProfileRepository.Update(profile);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminModerationActionResult>.Success(new AdminModerationActionResult(
            user.Id,
            user.TelegramId,
            user.Language,
            profile?.Name ?? user.FirstName
        ));
    }

    public async Task<Result<AdminModerationActionResult>> DeleteUserProfileDirectlyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<AdminModerationActionResult>.Failure("Пользователь не найден.");
        }

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is not null)
        {
            profile.Gender = null;
            profile.TargetGender = null;
            profile.Name = null;
            profile.Age = null;
            profile.City = null;
            profile.CityId = null;
            profile.Height = null;
            profile.PhotoFileId = null;
            profile.DatingTarget = null;
            profile.AiDescription = null;
            profile.AiVector = null;
            profile.Greeting = null;
            profile.IsCompleted = false;
            profile.UpdatedAt = DateTime.UtcNow;

            await userProfileRepository.SetInterestsAsync(profile.Id, [], cancellationToken);
            userProfileRepository.Update(profile);
        }

        user.State = UserState.Registration_SelectingLanguage;
        user.CurrentCandidateProfileId = null;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminModerationActionResult>.Success(new AdminModerationActionResult(
            user.Id,
            user.TelegramId,
            user.Language,
            user.FirstName ?? "Пользователь"
        ));
    }
}
