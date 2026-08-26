using DatingBot.Domain.Enums;

namespace DatingBot.Application.DTOs;

public record ReferralLinkDto(
    Guid Id,
    string Code,
    string LinkUrl,
    int InvitedCount,
    DateTime CreatedAt
);

public record ReferralProcessedDto(
    long ReferrerTelegramId,
    AppLanguage ReferrerLanguage,
    int TotalBoostDays
);
