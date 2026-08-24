using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Validators;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Services;

public class ProfileEditingService(
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IInterestRepository interestRepository,
    ICityRepository cityRepository,
    IAiEmbeddingService aiEmbeddingService,
    ILocalizationService loc,
    IUnitOfWork unitOfWork) : IProfileEditingService
{
    private readonly NameValidator _nameValidator = new();
    private readonly AgeValidator _ageValidator = new();
    private readonly CityValidator _cityValidator = new();
    private readonly HeightValidator _heightValidator = new();
    private readonly AiDescriptionValidator _aiDescriptionValidator = new();
    private readonly GreetingValidator _greetingValidator = new();

    public async Task<Result> SetEditingStateAsync(long telegramId, UserState editingState, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        user.State = editingState;
        user.UpdatedAt = DateTime.UtcNow;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CancelEditingAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateLanguageAsync(long telegramId, AppLanguage language, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        user.Language = language;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateNameAsync(long telegramId, string name, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var validation = await _nameValidator.ValidateAsync(name, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(loc.Get(lang, validation.Errors[0].ErrorMessage));
        }

        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.Name = name.Trim();
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateAgeAsync(long telegramId, int age, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var validation = await _ageValidator.ValidateAsync(age, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(loc.Get(lang, validation.Errors[0].ErrorMessage));
        }

        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        // Если пользователю стало меньше 18 лет, а цель была 18+ — сбрасываем цель на Relationship
        if (age < 18 && profile.DatingTarget == DatingTarget.AdultOnly)
        {
            profile.DatingTarget = DatingTarget.Relationship;
        }

        profile.Age = age;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateCityAsync(long telegramId, string city, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var validation = await _cityValidator.ValidateAsync(city, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(loc.Get(lang, validation.Errors[0].ErrorMessage));
        }

        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        var cityRecord = await cityRepository.FindExactByNameAsync(city.Trim(), cancellationToken);
        if (cityRecord is not null)
        {
            profile.City = cityRecord.Name;
            profile.CityId = cityRecord.Id;
        }
        else
        {
            profile.City = city.Trim();
            profile.CityId = null;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateHeightAsync(long telegramId, int? height, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        if (height.HasValue)
        {
            var validation = await _heightValidator.ValidateAsync(height.Value, cancellationToken);
            if (!validation.IsValid)
            {
                return Result.Failure(loc.Get(lang, validation.Errors[0].ErrorMessage));
            }
        }

        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.Height = height;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdatePhotoAsync(long telegramId, string fileId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        if (string.IsNullOrWhiteSpace(fileId))
        {
            return Result.Failure(loc.Get(lang, "Error_PhotoRequired"));
        }

        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.PhotoFileId = fileId;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateGenderAsync(long telegramId, Gender gender, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.Gender = gender;
        profile.UpdatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateTargetGenderAsync(long telegramId, TargetGender targetGender, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.TargetGender = targetGender;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateDatingTargetAsync(long telegramId, DatingTarget target, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        if (target == DatingTarget.AdultOnly && profile.Age < 18)
        {
            return Result.Failure(loc.Get(user.Language, "Error_AdultOnlyUnder18"));
        }

        profile.DatingTarget = target;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<InterestDto>>> ToggleEditInterestAsync(long telegramId, InterestType code, CancellationToken cancellationToken = default)
    {
        var interest = await interestRepository.GetByCodeAsync(code, cancellationToken);
        if (interest is null) return Result<IReadOnlyList<InterestDto>>.Failure("Interest not found.");

        var profile = await userProfileRepository.GetWithInterestsByTelegramIdAsync(telegramId, cancellationToken);
        if (profile is null) return Result<IReadOnlyList<InterestDto>>.Failure(loc.Get(AppLanguage.Russian, "Error_ProfileNotFound"));

        var currentSelectedIds = profile.Interests.Select(i => i.InterestId).ToHashSet();
        if (currentSelectedIds.Contains(interest.Id))
        {
            currentSelectedIds.Remove(interest.Id);
        }
        else
        {
            currentSelectedIds.Add(interest.Id);
        }

        await userProfileRepository.SetInterestsAsync(profile.Id, currentSelectedIds, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var allInterests = await interestRepository.GetAllAsync(cancellationToken);
        var updatedDtos = allInterests
            .Select(i => new InterestDto(i.Id, i.Code, i.Title, i.Icon, currentSelectedIds.Contains(i.Id)))
            .ToList();

        return Result<IReadOnlyList<InterestDto>>.Success(updatedDtos);
    }

    public async Task<Result> SaveEditInterestsAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetWithInterestsByTelegramIdAsync(telegramId, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        if (profile.Interests.Count == 0)
        {
            return Result.Failure(loc.Get(user.Language, "Error_InterestsMin"));
        }

        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<AgeCategoryFilter>> ToggleAgeCategoryAsync(long telegramId, AgeCategoryFilter category, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result<AgeCategoryFilter>.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result<AgeCategoryFilter>.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        // Переключаем битовый флаг
        if (profile.AgeFilters.HasFlag(category))
        {
            profile.AgeFilters &= ~category;
        }
        else
        {
            profile.AgeFilters |= category;
        }

        // При выборе категорий сбрасываем ручной диапазон
        profile.SearchMinAge = null;
        profile.SearchMaxAge = null;
        profile.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AgeCategoryFilter>.Success(profile.AgeFilters);
    }

    public async Task<Result> SaveAgeCategoriesAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetSearchMinAgeAsync(long telegramId, int minAge, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var validation = await _ageValidator.ValidateAsync(minAge, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(loc.Get(lang, validation.Errors[0].ErrorMessage));
        }

        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.SearchMinAge = minAge;
        profile.AgeFilters = AgeCategoryFilter.None; // сбрасываем категории при ручном вводе
        profile.UpdatedAt = DateTime.UtcNow;

        user.State = UserState.Editing_SearchMaxAge;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetSearchMaxAgeAsync(long telegramId, int maxAge, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var validation = await _ageValidator.ValidateAsync(maxAge, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(loc.Get(lang, validation.Errors[0].ErrorMessage));
        }

        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        if (profile.SearchMinAge.HasValue && maxAge < profile.SearchMinAge.Value)
        {
            return Result.Failure(loc.Get(user.Language, "Error_AgeMaxLessThanMin", profile.SearchMinAge.Value));
        }

        profile.SearchMaxAge = maxAge;
        profile.UpdatedAt = DateTime.UtcNow;

        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateAiBioAsync(long telegramId, string description, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var validation = await _aiDescriptionValidator.ValidateAsync(description, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(loc.Get(lang, validation.Errors[0].ErrorMessage));
        }

        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.AiDescription = description.Trim();
        var vector = await aiEmbeddingService.GenerateEmbeddingAsync(description.Trim(), cancellationToken);
        if (vector is not null)
        {
            profile.AiVector = aiEmbeddingService.VectorToBytes(vector);
        }

        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateGreetingAsync(long telegramId, string greeting, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var validation = await _greetingValidator.ValidateAsync(greeting, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(loc.Get(lang, validation.Errors[0].ErrorMessage));
        }

        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.Greeting = greeting.Trim();
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateSearchDistanceAsync(long telegramId, SearchDistancePreference searchDistance, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.SearchDistance = searchDistance;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
