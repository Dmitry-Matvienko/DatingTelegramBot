using DatingBot.Domain.Enums;

namespace DatingBot.Application.DTOs;

public record ReferralLinkDto(
    Guid Id,
    string Code,
    string LinkUrl,
    int InvitedCount,
    DateTime CreatedAt,
    int RemainingBoostDays = 0
);

public record ReferralProcessedDto(
    long ReferrerTelegramId,
    AppLanguage ReferrerLanguage,
    int TotalBoostDays
);

public record ReferralTopUserDto(
    Guid UserId,
    long TelegramId,
    string? Username,
    string? Name,
    int InvitedCount
);
