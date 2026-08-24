using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Interfaces;

public interface IRegistrationService
{
    Task<User> GetOrCreateUserAsync(long telegramId, string? username, string? firstName, CancellationToken cancellationToken = default);
    Task<Result> SetLanguageAsync(long telegramId, AppLanguage language, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetProfileDtoAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InterestDto>> GetUserInterestsAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result> SetGenderAsync(long telegramId, Gender gender, CancellationToken cancellationToken = default);
    Task<Result> SetTargetGenderAsync(long telegramId, TargetGender targetGender, CancellationToken cancellationToken = default);
    Task<Result> SetNameAsync(long telegramId, string name, CancellationToken cancellationToken = default);
    Task<Result> SetAgeAsync(long telegramId, int age, CancellationToken cancellationToken = default);
    Task<Result> SetCityAsync(long telegramId, string city, CancellationToken cancellationToken = default);
    Task<Result> SetHeightAsync(long telegramId, int height, CancellationToken cancellationToken = default);
    Task<Result> SkipHeightAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result> SetPhotoAsync(long telegramId, string fileId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InterestDto>>> ToggleInterestAsync(long telegramId, InterestType code, CancellationToken cancellationToken = default);
    Task<Result> CompleteInterestsAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result> SetDatingTargetAsync(long telegramId, DatingTarget target, CancellationToken cancellationToken = default);
    Task<Result> SetAiDescriptionAsync(long telegramId, string description, CancellationToken cancellationToken = default);
    Task<Result<UserProfileDto>> SetSearchDistanceAndCompleteAsync(long telegramId, SearchDistancePreference searchDistance, CancellationToken cancellationToken = default);
    Task<Result<UserProfileDto>> SetAiDescriptionAndCompleteAsync(long telegramId, string description, CancellationToken cancellationToken = default);
    Task<Result> ResetRegistrationAsync(long telegramId, CancellationToken cancellationToken = default);
    Task SaveLastBotMessageIdAsync(long telegramId, int? messageId, CancellationToken cancellationToken = default);
}
