using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Interfaces;

public interface IProfileEditingService
{
    Task<Result> SetEditingStateAsync(long telegramId, UserState editingState, CancellationToken cancellationToken = default);
    Task<Result> CancelEditingAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result> UpdateLanguageAsync(long telegramId, AppLanguage language, CancellationToken cancellationToken = default);
    Task<Result> UpdateNameAsync(long telegramId, string name, CancellationToken cancellationToken = default);
    Task<Result> UpdateAgeAsync(long telegramId, int age, CancellationToken cancellationToken = default);
    Task<Result> UpdateCityAsync(long telegramId, string city, CancellationToken cancellationToken = default);
    Task<Result> UpdateHeightAsync(long telegramId, int? height, CancellationToken cancellationToken = default);
    Task<Result> UpdatePhotoAsync(long telegramId, string fileId, CancellationToken cancellationToken = default);
    Task<Result> UpdateGenderAsync(long telegramId, Gender gender, CancellationToken cancellationToken = default);
    Task<Result> UpdateTargetGenderAsync(long telegramId, TargetGender targetGender, CancellationToken cancellationToken = default);
    Task<Result> UpdateDatingTargetAsync(long telegramId, DatingTarget target, CancellationToken cancellationToken = default);
    Task<Result> UpdateAiBioAsync(long telegramId, string bio, CancellationToken cancellationToken = default);
    Task<Result> UpdateGreetingAsync(long telegramId, string greeting, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InterestDto>>> ToggleEditInterestAsync(long telegramId, InterestType code, CancellationToken cancellationToken = default);
    Task<Result> SaveEditInterestsAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result<AgeCategoryFilter>> ToggleAgeCategoryAsync(long telegramId, AgeCategoryFilter category, CancellationToken cancellationToken = default);
    Task<Result> SaveAgeCategoriesAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<Result> SetSearchMinAgeAsync(long telegramId, int minAge, CancellationToken cancellationToken = default);
    Task<Result> SetSearchMaxAgeAsync(long telegramId, int maxAge, CancellationToken cancellationToken = default);
}
