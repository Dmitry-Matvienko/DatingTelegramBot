using DatingBot.Application.DTOs;
using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Domain.Enums;
using DatingBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingBot.Infrastructure.Repositories;

public class UserProfileRepository(AppDbContext dbContext) : IUserProfileRepository
{
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserProfiles
            .Include(p => p.User)
            .Include(p => p.CityRef)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<UserProfile?> GetByIdAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserProfiles
            .Include(p => p.User)
            .Include(p => p.CityRef)
            .Include(p => p.Interests)
                .ThenInclude(i => i.Interest)
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);
    }

    public async Task<UserProfile?> GetWithInterestsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserProfiles
            .Include(p => p.User)
            .Include(p => p.CityRef)
            .Include(p => p.Interests)
                .ThenInclude(i => i.Interest)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<UserProfile?> GetNextCandidateForUserAsync(UserProfile currentUserProfile, HashSet<Guid> excludedUserIds, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UserProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.CityRef)
            .Include(p => p.Interests)
                .ThenInclude(i => i.Interest)
            .Where(p => p.IsCompleted)
            .Where(p => p.User.State == UserState.Active || p.User.State == UserState.Searching)
            .Where(p => !excludedUserIds.Contains(p.UserId));

        // Строгая изоляция по стране (пользователь из Украины не видит РФ, Бразилия не видит другие страны и т.д.)
        if (!string.IsNullOrEmpty(currentUserProfile.CityRef?.Country))
        {
            var userCountry = currentUserProfile.CityRef.Country;
            query = query.Where(p => p.CityRef != null && p.CityRef.Country == userCountry);
        }

        // Фильтрация по полу: кого ищет текущий пользователь
        if (currentUserProfile.TargetGender == TargetGender.Male)
        {
            query = query.Where(p => p.Gender == Gender.Male);
        }
        else if (currentUserProfile.TargetGender == TargetGender.Female)
        {
            query = query.Where(p => p.Gender == Gender.Female);
        }

        // Фильтрация по полу: подходит ли текущий пользователь кандидату
        if (currentUserProfile.Gender == Gender.Male)
        {
            query = query.Where(p => p.TargetGender == TargetGender.Male || p.TargetGender == TargetGender.All);
        }
        else if (currentUserProfile.Gender == Gender.Female)
        {
            query = query.Where(p => p.TargetGender == TargetGender.Female || p.TargetGender == TargetGender.All);
        }

        // Фильтрация по возрасту (ручной диапазон)
        if (currentUserProfile.SearchMinAge.HasValue)
        {
            query = query.Where(p => p.Age >= currentUserProfile.SearchMinAge.Value);
        }
        if (currentUserProfile.SearchMaxAge.HasValue)
        {
            query = query.Where(p => p.Age <= currentUserProfile.SearchMaxAge.Value);
        }

        // Фильтрация по возрастным категориям (если заданы)
        if (currentUserProfile.AgeFilters != AgeCategoryFilter.None)
        {
            var under18 = currentUserProfile.AgeFilters.HasFlag(AgeCategoryFilter.Under18);
            var age18To25 = currentUserProfile.AgeFilters.HasFlag(AgeCategoryFilter.Age18To25);
            var age25To30 = currentUserProfile.AgeFilters.HasFlag(AgeCategoryFilter.Age25To30);
            var age30To40 = currentUserProfile.AgeFilters.HasFlag(AgeCategoryFilter.Age30To40);
            var age40Plus = currentUserProfile.AgeFilters.HasFlag(AgeCategoryFilter.Age40Plus);

            query = query.Where(p =>
                (under18 && p.Age < 18) ||
                (age18To25 && p.Age >= 18 && p.Age <= 25) ||
                (age25To30 && p.Age >= 25 && p.Age <= 30) ||
                (age30To40 && p.Age >= 30 && p.Age <= 40) ||
                (age40Plus && p.Age >= 40)
            );
        }

        // Строгая изоляция по цели знакомства («Общение» -> «Общение», «Отношения» -> «Отношения», «18+» -> «18+»)
        query = query.Where(p => p.DatingTarget == currentUserProfile.DatingTarget);

        // Изоляция по языковым группам с учетом страны нахождения пользователя
        var compatibleLanguages = GetCompatibleLanguages(currentUserProfile.User?.Language ?? AppLanguage.Russian, currentUserProfile.CityRef?.Country);
        query = query.Where(p => compatibleLanguages.Contains(p.User.Language));

        // Безопасность 18+: если текущий пользователь младше 18, кандидат не может быть 18+ целевой
        if (currentUserProfile.Age < 18)
        {
            query = query.Where(p => p.DatingTarget != DatingTarget.AdultOnly && p.Age < 18);
        }

        return await query
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserProfile>> GetEligibleCandidatesAsync(UserProfile currentUserProfile, HashSet<Guid> excludedUserIds, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UserProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.CityRef)
            .Include(p => p.Interests)
                .ThenInclude(i => i.Interest)
            .Where(p => p.IsCompleted)
            .Where(p => p.User.State == UserState.Active || p.User.State == UserState.Searching)
            .Where(p => !excludedUserIds.Contains(p.UserId));

        // Строгая изоляция по стране (пользователь из Украины не видит РФ, Бразилия не видит другие страны и т.д.)
        if (!string.IsNullOrEmpty(currentUserProfile.CityRef?.Country))
        {
            var userCountry = currentUserProfile.CityRef.Country;
            query = query.Where(p => p.CityRef != null && p.CityRef.Country == userCountry);
        }

        // Фильтрация по полу: кого ищет текущий пользователь
        if (currentUserProfile.TargetGender == TargetGender.Male)
        {
            query = query.Where(p => p.Gender == Gender.Male);
        }
        else if (currentUserProfile.TargetGender == TargetGender.Female)
        {
            query = query.Where(p => p.Gender == Gender.Female);
        }

        // Фильтрация по полу: подходит ли текущий пользователь кандидату
        if (currentUserProfile.Gender == Gender.Male)
        {
            query = query.Where(p => p.TargetGender == TargetGender.Male || p.TargetGender == TargetGender.All);
        }
        else if (currentUserProfile.Gender == Gender.Female)
        {
            query = query.Where(p => p.TargetGender == TargetGender.Female || p.TargetGender == TargetGender.All);
        }

        // Фильтрация по возрасту (ручной диапазон)
        if (currentUserProfile.SearchMinAge.HasValue)
        {
            query = query.Where(p => p.Age >= currentUserProfile.SearchMinAge.Value);
        }
        if (currentUserProfile.SearchMaxAge.HasValue)
        {
            query = query.Where(p => p.Age <= currentUserProfile.SearchMaxAge.Value);
        }

        // Фильтрация по возрастным категориям (если заданы)
        if (currentUserProfile.AgeFilters != AgeCategoryFilter.None)
        {
            var under18 = currentUserProfile.AgeFilters.HasFlag(AgeCategoryFilter.Under18);
            var age18To25 = currentUserProfile.AgeFilters.HasFlag(AgeCategoryFilter.Age18To25);
            var age25To30 = currentUserProfile.AgeFilters.HasFlag(AgeCategoryFilter.Age25To30);
            var age30To40 = currentUserProfile.AgeFilters.HasFlag(AgeCategoryFilter.Age30To40);
            var age40Plus = currentUserProfile.AgeFilters.HasFlag(AgeCategoryFilter.Age40Plus);

            query = query.Where(p =>
                (under18 && p.Age < 18) ||
                (age18To25 && p.Age >= 18 && p.Age <= 25) ||
                (age25To30 && p.Age >= 25 && p.Age <= 30) ||
                (age30To40 && p.Age >= 30 && p.Age <= 40) ||
                (age40Plus && p.Age >= 40)
            );
        }

        // Строгая изоляция по цели знакомства («Общение» -> «Общение», «Отношения» -> «Отношения», «18+» -> «18+»)
        query = query.Where(p => p.DatingTarget == currentUserProfile.DatingTarget);

        // Изоляция по языковым группам с учетом страны нахождения пользователя
        var compatibleLanguages = GetCompatibleLanguages(currentUserProfile.User?.Language ?? AppLanguage.Russian, currentUserProfile.CityRef?.Country);
        query = query.Where(p => compatibleLanguages.Contains(p.User.Language));

        // Безопасность 18+: если текущий пользователь младше 18, кандидат не может быть 18+ целевой
        if (currentUserProfile.Age < 18)
        {
            query = query.Where(p => p.DatingTarget != DatingTarget.AdultOnly && p.Age < 18);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public static List<AppLanguage> GetCompatibleLanguages(AppLanguage language, string? userCountry = null)
    {
        var result = language switch
        {
            AppLanguage.Hindi => new HashSet<AppLanguage> { AppLanguage.Hindi, AppLanguage.English },
            AppLanguage.Portuguese => new HashSet<AppLanguage> { AppLanguage.Portuguese, AppLanguage.English },
            AppLanguage.Indonesian => new HashSet<AppLanguage> { AppLanguage.Indonesian, AppLanguage.English },
            AppLanguage.Russian => new HashSet<AppLanguage> { AppLanguage.Russian, AppLanguage.Ukrainian, AppLanguage.English },
            AppLanguage.Ukrainian => new HashSet<AppLanguage> { AppLanguage.Ukrainian, AppLanguage.Russian, AppLanguage.English },
            AppLanguage.English => new HashSet<AppLanguage> {
                AppLanguage.English,
                AppLanguage.Russian,
                AppLanguage.Ukrainian,
                AppLanguage.Hindi,
                AppLanguage.Portuguese,
                AppLanguage.Indonesian
            },
            _ => new HashSet<AppLanguage> { language, AppLanguage.English }
        };

        // Если пользователь находится в определенной стране, он также может видеть анкеты на местном языке этой страны
        if (!string.IsNullOrEmpty(userCountry))
        {
            var countryClean = userCountry.Trim().ToLowerInvariant();
            if (countryClean.Contains("бразил") || countryClean.Contains("португал") || countryClean.Contains("brazil") || countryClean.Contains("portugal"))
            {
                result.Add(AppLanguage.Portuguese);
            }
            else if (countryClean.Contains("инди") || countryClean.Contains("india"))
            {
                result.Add(AppLanguage.Hindi);
            }
            else if (countryClean.Contains("индонез") || countryClean.Contains("indonesia"))
            {
                result.Add(AppLanguage.Indonesian);
            }
            else if (countryClean.Contains("украин") || countryClean.Contains("ukraine"))
            {
                result.Add(AppLanguage.Ukrainian);
            }
            else if (countryClean.Contains("росси") || countryClean.Contains("russia") || countryClean.Contains("беларус") || countryClean.Contains("казах"))
            {
                result.Add(AppLanguage.Russian);
            }
        }

        return result.ToList();
    }

    public async Task<int> ResetRatingsForCityAsync(Guid userId, int? cityId, string? cityName, CancellationToken cancellationToken = default)
    {
        var ratingsQuery = dbContext.ProfileRatings
            .Where(r => r.FromUserId == userId && r.Score <= 5);

        if (cityId.HasValue)
        {
            ratingsQuery = ratingsQuery.Where(r => r.ToUser.Profile != null && (r.ToUser.Profile.CityId == cityId || r.ToUser.Profile.City == cityName));
        }
        else if (!string.IsNullOrEmpty(cityName))
        {
            ratingsQuery = ratingsQuery.Where(r => r.ToUser.Profile != null && r.ToUser.Profile.City == cityName);
        }

        var ratingsToReset = await ratingsQuery.ToListAsync(cancellationToken);
        if (ratingsToReset.Count == 0) return 0;

        dbContext.ProfileRatings.RemoveRange(ratingsToReset);
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfile?> GetWithInterestsByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await dbContext.UserProfiles
            .Include(p => p.User)
            .Include(p => p.CityRef)
            .Include(p => p.Interests)
                .ThenInclude(i => i.Interest)
            .FirstOrDefaultAsync(p => p.User.TelegramId == telegramId, cancellationToken);
    }

    public async Task AddAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await dbContext.UserProfiles.AddAsync(profile, cancellationToken);
    }

    public void Update(UserProfile profile)
    {
        dbContext.UserProfiles.Update(profile);
    }

    public async Task SetInterestsAsync(Guid userProfileId, IEnumerable<int> interestIds, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.UserProfileInterests
            .Where(upi => upi.UserProfileId == userProfileId)
            .ToListAsync(cancellationToken);

        dbContext.UserProfileInterests.RemoveRange(existing);

        var newInterests = interestIds.Select(id => new UserProfileInterest
        {
            UserProfileId = userProfileId,
            InterestId = id
        });

        await dbContext.UserProfileInterests.AddRangeAsync(newInterests, cancellationToken);
    }

    public async Task<(UserProfile? Profile, int TotalCount)> GetAdminProfileByGenderAsync(Gender gender, int offset, CancellationToken cancellationToken = default)
    {
        var query = dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.Gender == gender && p.IsCompleted && p.User.State != UserState.Banned);

        var totalCount = await query.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return (null, 0);
        }

        var normalizedOffset = ((offset % totalCount) + totalCount) % totalCount;

        var profile = await query
            .Include(p => p.User)
            .Include(p => p.CityRef)
            .Include(p => p.Interests)
                .ThenInclude(i => i.Interest)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(normalizedOffset)
            .Take(1)
            .FirstOrDefaultAsync(cancellationToken);

        return (profile, totalCount);
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await dbContext.Users.AsNoTracking().CountAsync(cancellationToken);
        var completedProfiles = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.IsCompleted, cancellationToken);
        var bannedUsers = await dbContext.Users.AsNoTracking().CountAsync(u => u.State == UserState.Banned, cancellationToken);
        var maleCount = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.Gender == Gender.Male, cancellationToken);
        var femaleCount = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.Gender == Gender.Female, cancellationToken);

        var now = DateTime.UtcNow;
        var newUsers24h = await dbContext.Users.AsNoTracking().CountAsync(u => u.CreatedAt >= now.AddHours(-24), cancellationToken);
        var newUsers7d = await dbContext.Users.AsNoTracking().CountAsync(u => u.CreatedAt >= now.AddDays(-7), cancellationToken);
        var newUsers30d = await dbContext.Users.AsNoTracking().CountAsync(u => u.CreatedAt >= now.AddDays(-30), cancellationToken);

        var friends = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.DatingTarget == DatingTarget.Friends, cancellationToken);
        var relationship = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.DatingTarget == DatingTarget.Relationship, cancellationToken);
        var adultOnly = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.DatingTarget == DatingTarget.AdultOnly, cancellationToken);

        var ageUnder18 = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.Age.HasValue && p.Age.Value < 18, cancellationToken);
        var age18To24 = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.Age.HasValue && p.Age.Value >= 18 && p.Age.Value <= 24, cancellationToken);
        var age25To34 = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.Age.HasValue && p.Age.Value >= 25 && p.Age.Value <= 34, cancellationToken);
        var age35To44 = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.Age.HasValue && p.Age.Value >= 35 && p.Age.Value <= 44, cancellationToken);
        var age45Plus = await dbContext.UserProfiles.AsNoTracking().CountAsync(p => p.Age.HasValue && p.Age.Value >= 45, cancellationToken);

        var topCities = await GetTopCitiesStatsAsync(5, cancellationToken);

        var topCountriesData = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.CityRef != null && !string.IsNullOrEmpty(p.CityRef.Country))
            .GroupBy(p => p.CityRef!.Country!)
            .Select(g => new { Country = g.Key, Count = g.Count() })
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Country)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topCountries = topCountriesData
            .Select(c => new AdminCountryStatsDto(c.Country, c.Count))
            .ToList();

        return new AdminStatsDto(
            totalUsers,
            completedProfiles,
            bannedUsers,
            maleCount,
            femaleCount,
            newUsers24h,
            newUsers7d,
            newUsers30d,
            friends,
            relationship,
            adultOnly,
            ageUnder18,
            age18To24,
            age25To34,
            age35To44,
            age45Plus,
            topCities,
            topCountries
        );
    }

    public async Task<IReadOnlyList<AdminCityStatsDto>> GetTopCitiesStatsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var topCityNames = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => !string.IsNullOrEmpty(p.City))
            .GroupBy(p => p.City!)
            .Select(g => new { CityName = g.Key, UserCount = g.Count() })
            .OrderByDescending(x => x.UserCount)
            .ThenBy(x => x.CityName)
            .Take(count)
            .ToListAsync(cancellationToken);

        if (topCityNames.Count == 0)
        {
            return [];
        }

        var cityNames = topCityNames.Select(x => x.CityName).ToList();

        var profilesInTopCities = await dbContext.UserProfiles
            .AsNoTracking()
            .Include(p => p.CityRef)
            .Where(p => p.City != null && cityNames.Contains(p.City))
            .Select(p => new
            {
                City = p.City!,
                Country = p.CityRef != null ? p.CityRef.Country : null,
                p.IsCompleted,
                p.Gender
            })
            .ToListAsync(cancellationToken);

        var result = topCityNames.Select(top =>
        {
            var inCity = profilesInTopCities.Where(p => p.City == top.CityName).ToList();
            var country = inCity.FirstOrDefault(p => !string.IsNullOrEmpty(p.Country))?.Country;
            var completed = inCity.Count(p => p.IsCompleted);
            var male = inCity.Count(p => p.Gender == Gender.Male);
            var female = inCity.Count(p => p.Gender == Gender.Female);
            return new AdminCityStatsDto(top.CityName, country, inCity.Count, completed, male, female);
        }).ToList();

        return result;
    }

    public async Task<AdminCityStatsDto?> GetCityStatsAsync(string cityName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return null;

        var cleanCity = cityName.Trim().ToLower();

        var profiles = await dbContext.UserProfiles
            .AsNoTracking()
            .Include(p => p.CityRef)
            .Where(p => p.City != null && (p.City.ToLower() == cleanCity || (p.CityRef != null && p.CityRef.Name.ToLower() == cleanCity)))
            .Select(p => new
            {
                p.City,
                Country = p.CityRef != null ? p.CityRef.Country : null,
                p.IsCompleted,
                p.Gender
            })
            .ToListAsync(cancellationToken);

        if (profiles.Count == 0) return null;

        var displayName = profiles.FirstOrDefault(p => !string.IsNullOrEmpty(p.City))?.City ?? cityName;
        var country = profiles.FirstOrDefault(p => !string.IsNullOrEmpty(p.Country))?.Country;

        return new AdminCityStatsDto(
            displayName,
            country,
            profiles.Count,
            profiles.Count(p => p.IsCompleted),
            profiles.Count(p => p.Gender == Gender.Male),
            profiles.Count(p => p.Gender == Gender.Female)
        );
    }

    public async Task<IReadOnlyList<long>> GetBroadcastRecipientsAsync(AdminBroadcastFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .Where(u => u.State != UserState.Banned);

        if (filter.TargetGender.HasValue)
        {
            query = query.Where(u => u.Profile != null && u.Profile.Gender == filter.TargetGender.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var cleanCity = filter.City.Trim().ToLower();
            query = query.Where(u => u.Profile != null && u.Profile.City != null && (u.Profile.City.ToLower() == cleanCity || (u.Profile.CityRef != null && u.Profile.CityRef.Name.ToLower() == cleanCity)));
        }

        if (filter.MinAge.HasValue)
        {
            query = query.Where(u => u.Profile != null && u.Profile.Age >= filter.MinAge.Value);
        }

        if (filter.MaxAge.HasValue)
        {
            query = query.Where(u => u.Profile != null && u.Profile.Age <= filter.MaxAge.Value);
        }

        if (filter.TargetGoal.HasValue)
        {
            query = query.Where(u => u.Profile != null && u.Profile.DatingTarget == filter.TargetGoal.Value);
        }

        return await query
            .Select(u => u.TelegramId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
