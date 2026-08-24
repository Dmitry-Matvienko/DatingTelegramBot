using DatingBot.Domain.Enums;

namespace DatingBot.Application.DTOs;

public record UserProfileDto(
    Guid Id,
    long TelegramId,
    string? Username,
    Gender? Gender,
    TargetGender? TargetGender,
    string? Name,
    int? Age,
    string? City,
    int? Height,
    string? PhotoFileId,
    DatingTarget? DatingTarget,
    string? AiDescription,
    IReadOnlyList<InterestDto> SelectedInterests,
    bool IsCompleted,
    AgeCategoryFilter AgeFilters = AgeCategoryFilter.None,
    int? SearchMinAge = null,
    int? SearchMaxAge = null,
    int RatingCount = 0,
    double AverageRating = 0.0,
    int? CityId = null,
    byte[]? AiVector = null,
    string? Greeting = null,
    SearchDistancePreference SearchDistance = SearchDistancePreference.UpTo500Km
);
