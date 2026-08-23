using DatingBot.Application.Common;
using DatingBot.Application.Interfaces;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Services;

public class ModerationService(
    IProfileReportRepository profileReportRepository,
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IUnitOfWork unitOfWork) : IModerationService
{
    public async Task<Result<ModerationActionResult>> BanUserByReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await profileReportRepository.GetByIdWithUsersAsync(reportId, cancellationToken);
        if (report is null)
        {
            return Result<ModerationActionResult>.Failure("Жалоба не найдена.");
        }

        var reportedUser = report.ReportedUser;
        var reporter = report.Reporter;

        if (reportedUser.State == UserState.Banned)
        {
            return Result<ModerationActionResult>.Failure("Пользователь уже заблокирован.");
        }

        reportedUser.State = UserState.Banned;
        reportedUser.CurrentCandidateProfileId = null;
        reportedUser.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(reportedUser);

        if (reportedUser.Profile is not null)
        {
            reportedUser.Profile.IsCompleted = false;
            reportedUser.Profile.UpdatedAt = DateTime.UtcNow;
            userProfileRepository.Update(reportedUser.Profile);
        }

        report.IsResolved = true;
        report.ResolvedAt = DateTime.UtcNow;
        profileReportRepository.Update(report);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ModerationActionResult>.Success(new ModerationActionResult(
            report.Id,
            reporter.TelegramId,
            reporter.Language,
            reportedUser.TelegramId,
            reportedUser.Language,
            reportedUser.Profile?.Name ?? reportedUser.FirstName,
            true
        ));
    }

    public async Task<Result<ModerationActionResult>> DeleteProfileByReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await profileReportRepository.GetByIdWithUsersAsync(reportId, cancellationToken);
        if (report is null)
        {
            return Result<ModerationActionResult>.Failure("Жалоба не найдена.");
        }

        var reportedUser = report.ReportedUser;
        var reporter = report.Reporter;

        if (reportedUser.Profile is not null)
        {
            var profile = reportedUser.Profile;
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

        reportedUser.State = UserState.Registration_SelectingLanguage;
        reportedUser.CurrentCandidateProfileId = null;
        reportedUser.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(reportedUser);

        report.IsResolved = true;
        report.ResolvedAt = DateTime.UtcNow;
        profileReportRepository.Update(report);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ModerationActionResult>.Success(new ModerationActionResult(
            report.Id,
            reporter.TelegramId,
            reporter.Language,
            reportedUser.TelegramId,
            reportedUser.Language,
            reportedUser.FirstName ?? "Пользователь",
            true
        ));
    }

    public async Task<Result> IgnoreReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        var report = await profileReportRepository.GetByIdWithUsersAsync(reportId, cancellationToken);
        if (report is null)
        {
            return Result.Failure("Жалоба не найдена.");
        }

        report.IsResolved = true;
        report.ResolvedAt = DateTime.UtcNow;
        profileReportRepository.Update(report);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
