using DatingBot.Application.Common;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Interfaces;

public record ModerationActionResult(
    Guid ReportId,
    long ReporterTelegramId,
    AppLanguage ReporterLanguage,
    long ReportedTelegramId,
    AppLanguage ReportedLanguage,
    string? ReportedName,
    bool ShouldNotifyReporter
);

public interface IModerationService
{
    Task<Result<ModerationActionResult>> BanUserByReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<Result<ModerationActionResult>> DeleteProfileByReportAsync(Guid reportId, CancellationToken cancellationToken = default);
    Task<Result> IgnoreReportAsync(Guid reportId, CancellationToken cancellationToken = default);
}
