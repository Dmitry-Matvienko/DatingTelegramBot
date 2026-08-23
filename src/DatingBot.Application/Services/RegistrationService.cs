using DatingBot.Application.Common;
using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Application.Validators;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;

namespace DatingBot.Application.Services;

public class RegistrationService(
    IUserRepository userRepository,
    IUserProfileRepository userProfileRepository,
    IInterestRepository interestRepository,
    ICityRepository cityRepository,
    IAiEmbeddingService aiEmbeddingService,
    ILocalizationService loc,
    IUnitOfWork unitOfWork) : IRegistrationService
{
    private readonly NameValidator _nameValidator = new();
    private readonly AgeValidator _ageValidator = new();
    private readonly CityValidator _cityValidator = new();
    private readonly HeightValidator _heightValidator = new();
    private readonly AiDescriptionValidator _aiDescriptionValidator = new();

    public async Task<User> GetOrCreateUserAsync(long telegramId, string? username, string? firstName, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                TelegramId = telegramId,
                Username = username,
                FirstName = firstName,
                State = UserState.None,
                CreatedAt = DateTime.UtcNow
            };

            await userRepository.AddAsync(user, cancellationToken);

            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsCompleted = false
            };

            await userProfileRepository.AddAsync(profile, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Обновляем метаданные Telegram если они изменились
            if (user.Username != username || user.FirstName != firstName)
            {
                user.Username = username;
                user.FirstName = firstName;
                user.UpdatedAt = DateTime.UtcNow;
                userRepository.Update(user);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return user;
    }

    public async Task<Result> SetLanguageAsync(long telegramId, AppLanguage language, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        user.Language = language;
        user.State = UserState.Registration_SelectingGender;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<UserProfileDto?> GetProfileDtoAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var profile = await userProfileRepository.GetWithInterestsByTelegramIdAsync(telegramId, cancellationToken);
        if (profile is null) return null;

        var allInterests = await interestRepository.GetAllAsync(cancellationToken);
        var selectedInterestIds = profile.Interests.Select(i => i.InterestId).ToHashSet();

        var interestDtos = allInterests
            .Where(i => selectedInterestIds.Contains(i.Id))
            .Select(i => new InterestDto(i.Id, i.Code, i.Title, i.Icon, true))
            .ToList();

        return new UserProfileDto(
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
    }

    public async Task<IReadOnlyList<InterestDto>> GetUserInterestsAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var allInterests = await interestRepository.GetAllAsync(cancellationToken);
        var profile = await userProfileRepository.GetWithInterestsByTelegramIdAsync(telegramId, cancellationToken);

        var selectedIds = profile?.Interests.Select(i => i.InterestId).ToHashSet() ?? [];

        return allInterests
            .Select(i => new InterestDto(i.Id, i.Code, i.Title, i.Icon, selectedIds.Contains(i.Id)))
            .ToList();
    }

    public async Task<Result> SetGenderAsync(long telegramId, Gender gender, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.Gender = gender;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Registration_SelectingTargetGender;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetTargetGenderAsync(long telegramId, TargetGender targetGender, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.TargetGender = targetGender;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Registration_WaitingForName;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetNameAsync(long telegramId, string name, CancellationToken cancellationToken = default)
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
        user.State = UserState.Registration_WaitingForAge;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetAgeAsync(long telegramId, int age, CancellationToken cancellationToken = default)
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

        profile.Age = age;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Registration_WaitingForCity;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetCityAsync(long telegramId, string city, CancellationToken cancellationToken = default)
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
        user.State = UserState.Registration_WaitingForHeight;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetHeightAsync(long telegramId, int height, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var validation = await _heightValidator.ValidateAsync(height, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(loc.Get(lang, validation.Errors[0].ErrorMessage));
        }

        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.Height = height;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Registration_WaitingForPhoto;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SkipHeightAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.Height = null;
        profile.UpdatedAt = DateTime.UtcNow;
        user.State = UserState.Registration_WaitingForPhoto;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetPhotoAsync(long telegramId, string fileId, CancellationToken cancellationToken = default)
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
        user.State = UserState.Registration_SelectingInterests;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<InterestDto>>> ToggleInterestAsync(long telegramId, InterestType code, CancellationToken cancellationToken = default)
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

    public async Task<Result> CompleteInterestsAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        user.State = UserState.Registration_SelectingTarget;
        user.UpdatedAt = DateTime.UtcNow;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetDatingTargetAsync(long telegramId, DatingTarget target, CancellationToken cancellationToken = default)
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
        user.State = UserState.Registration_WaitingForAiBio;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<UserProfileDto>> SetAiDescriptionAndCompleteAsync(long telegramId, string description, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        var lang = user?.Language ?? AppLanguage.Russian;

        var validation = await _aiDescriptionValidator.ValidateAsync(description, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<UserProfileDto>.Failure(loc.Get(lang, validation.Errors[0].ErrorMessage));
        }

        if (user is null) return Result<UserProfileDto>.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetWithInterestsByUserIdAsync(user.Id, cancellationToken);
        if (profile is null) return Result<UserProfileDto>.Failure(loc.Get(user.Language, "Error_ProfileNotFound"));

        profile.AiDescription = description.Trim();
        var vector = await aiEmbeddingService.GenerateEmbeddingAsync(description.Trim(), cancellationToken);
        if (vector is not null)
        {
            profile.AiVector = aiEmbeddingService.VectorToBytes(vector);
        }

        profile.IsCompleted = true;
        profile.UpdatedAt = DateTime.UtcNow;

        user.State = UserState.Active;
        user.UpdatedAt = DateTime.UtcNow;

        userProfileRepository.Update(profile);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var allInterests = await interestRepository.GetAllAsync(cancellationToken);
        var selectedInterestIds = profile.Interests.Select(i => i.InterestId).ToHashSet();

        var interestDtos = allInterests
            .Where(i => selectedInterestIds.Contains(i.Id))
            .Select(i => new InterestDto(i.Id, i.Code, i.Title, i.Icon, true))
            .ToList();

        var dto = new UserProfileDto(
            profile.Id,
            user.TelegramId,
            user.Username,
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

        return Result<UserProfileDto>.Success(dto);
    }

    public async Task<Result> ResetRegistrationAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is null) return Result.Failure(loc.Get(AppLanguage.Russian, "Error_UserNotFound"));

        var profile = await userProfileRepository.GetWithInterestsByUserIdAsync(user.Id, cancellationToken);
        if (profile is not null)
        {
            profile.Gender = null;
            profile.TargetGender = null;
            profile.Name = null;
            profile.Age = null;
            profile.City = null;
            profile.Height = null;
            profile.PhotoFileId = null;
            profile.DatingTarget = null;
            profile.AiDescription = null;
            profile.Greeting = null;
            profile.IsCompleted = false;
            profile.UpdatedAt = DateTime.UtcNow;
            await userProfileRepository.SetInterestsAsync(profile.Id, [], cancellationToken);
            userProfileRepository.Update(profile);
        }

        user.State = UserState.Registration_SelectingLanguage;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task SaveLastBotMessageIdAsync(long telegramId, int? messageId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByTelegramIdAsync(telegramId, cancellationToken);
        if (user is not null)
        {
            user.LastBotMessageId = messageId;
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
