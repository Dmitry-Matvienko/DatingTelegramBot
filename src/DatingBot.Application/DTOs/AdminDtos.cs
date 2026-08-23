using DatingBot.Domain.Enums;

namespace DatingBot.Application.DTOs;

public record AdminStatsDto(
    int TotalUsers,
    int CompletedProfiles,
    int BannedUsers,
    int MaleCount,
    int FemaleCount,
    int NewUsersLast24Hours,
    int NewUsersLast7Days,
    int NewUsersLast30Days,
    int DatingTargetFriendsCount,
    int DatingTargetRelationshipCount,
    int DatingTargetAdultOnlyCount,
    int AgeUnder18Count,
    int Age18To24Count,
    int Age25To34Count,
    int Age35To44Count,
    int Age45PlusCount,
    IReadOnlyList<AdminCityStatsDto> TopCities,
    IReadOnlyList<AdminCountryStatsDto> TopCountries
);

public record AdminCityStatsDto(
    string CityName,
    string? Country,
    int UserCount,
    int CompletedCount,
    int MaleCount,
    int FemaleCount
);

public record AdminCountryStatsDto(
    string CountryName,
    int UserCount
);

public record AdminBroadcastFilterDto(
    Gender? TargetGender = null,
    string? City = null,
    int? MinAge = null,
    int? MaxAge = null,
    DatingTarget? TargetGoal = null
);

public record AdminBroadcastPreviewDto(
    string? Text,
    string? PhotoFileId,
    string? ButtonText,
    string? ButtonUrl,
    int TargetRecipientCount,
    AdminBroadcastFilterDto Filter
);

public record AdminBroadcastResultDto(
    int TotalTargets,
    int DeliveredCount,
    int FailedCount,
    TimeSpan Elapsed
);

public record AdminPendingReportDto(
    Guid ReportId,
    Guid ReporterUserId,
    long ReporterTelegramId,
    string? ReporterUsername,
    string? ReporterFirstName,
    AppLanguage ReporterLanguage,
    Guid ReportedUserId,
    UserProfileDto ReportedProfile,
    ReportReason Reason,
    string? Details,
    DateTime CreatedAt
);

public record AdminModerationActionResult(
    Guid UserId,
    long TelegramId,
    AppLanguage Language,
    string? Name
);
