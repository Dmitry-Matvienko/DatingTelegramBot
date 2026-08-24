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

    public async Task<UserProfile?> GetNextCandidateForUserAsync(UserProfile currentUserProfile, CancellationToken cancellationToken = default)
    {
        var currentUserId = currentUserProfile.UserId;
        var cycleStartedAt = currentUserProfile.User?.SearchCycleStartedAt ?? DateTime.MinValue;

        var query = BuildBaseCandidateQuery(currentUserProfile)
            .Include(p => p.User)
            .Include(p => p.CityRef)
            .Include(p => p.Interests)
                .ThenInclude(i => i.Interest)
            .Where(p => !dbContext.ProfileRatings.Any(r => r.FromUserId == currentUserId && r.ToUserId == p.UserId && r.CreatedAt > cycleStartedAt));

        return await query
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ThenBy(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserProfile>> GetEligibleCandidatesAsync(UserProfile currentUserProfile, int limit = 100, CancellationToken cancellationToken = default)
    {
        var currentUserId = currentUserProfile.UserId;
        var cycleStartedAt = currentUserProfile.User?.SearchCycleStartedAt ?? DateTime.MinValue;

        var query = BuildBaseCandidateQuery(currentUserProfile)
            .Include(p => p.User)
            .Include(p => p.CityRef)
            .Include(p => p.Interests)
                .ThenInclude(i => i.Interest)
            .Where(p => !dbContext.ProfileRatings.Any(r => r.FromUserId == currentUserId && r.ToUserId == p.UserId && r.CreatedAt > cycleStartedAt));

        return await query
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalEligibleCandidatesCountAsync(UserProfile currentUserProfile, CancellationToken cancellationToken = default)
    {
        return await BuildBaseCandidateQuery(currentUserProfile).CountAsync(cancellationToken);
    }

    private IQueryable<UserProfile> BuildBaseCandidateQuery(UserProfile currentUserProfile)
    {
        var currentUserId = currentUserProfile.UserId;

        var query = dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.IsCompleted)
            .Where(p => p.User.State == UserState.Active || p.User.State == UserState.Searching || p.User.State == UserState.Searching_ViewingIncoming)
            .Where(p => p.UserId != currentUserId)
            .Where(p => !dbContext.ProfileReports.Any(rep => rep.ReporterId == currentUserId && rep.ReportedUserId == p.UserId));

        // Строгая изоляция по стране (по умолчанию или для UpTo100Km, UpTo500Km, SameCountry)
        // Для Anywhere ограничение по стране отключается
        if (currentUserProfile.SearchDistance != SearchDistancePreference.Anywhere && !string.IsNullOrEmpty(currentUserProfile.CityRef?.Country))
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
            query = query.Where(p => p.DatingTarget != DatingTarget.AdultOnly);
        }

        return query;
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
        var now = DateTime.UtcNow;
        var cutoff24h = now.AddHours(-24);
        var cutoff7d = now.AddDays(-7);
        var cutoff30d = now.AddDays(-30);

        var userStats = await dbContext.Users
            .AsNoTracking()
            .Select(u => new
            {
                IsBanned = u.State == UserState.Banned ? 1 : 0,
                Is24h = u.CreatedAt >= cutoff24h ? 1 : 0,
                Is7d = u.CreatedAt >= cutoff7d ? 1 : 0,
                Is30d = u.CreatedAt >= cutoff30d ? 1 : 0
            })
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalUsers = g.Count(),
                BannedUsers = g.Sum(u => u.IsBanned),
                NewUsers24h = g.Sum(u => u.Is24h),
                NewUsers7d = g.Sum(u => u.Is7d),
                NewUsers30d = g.Sum(u => u.Is30d)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var profileStats = await dbContext.UserProfiles
            .AsNoTracking()
            .Select(p => new
            {
                IsCompleted = p.IsCompleted ? 1 : 0,
                IsMale = p.Gender == Gender.Male ? 1 : 0,
                IsFemale = p.Gender == Gender.Female ? 1 : 0,
                IsFriends = p.DatingTarget == DatingTarget.Friends ? 1 : 0,
                IsRelationship = p.DatingTarget == DatingTarget.Relationship ? 1 : 0,
                IsAdultOnly = p.DatingTarget == DatingTarget.AdultOnly ? 1 : 0,
                IsUnder18 = p.Age.HasValue && p.Age.Value < 18 ? 1 : 0,
                Is18To24 = p.Age.HasValue && p.Age.Value >= 18 && p.Age.Value <= 24 ? 1 : 0,
                Is25To34 = p.Age.HasValue && p.Age.Value >= 25 && p.Age.Value <= 34 ? 1 : 0,
                Is35To44 = p.Age.HasValue && p.Age.Value >= 35 && p.Age.Value <= 44 ? 1 : 0,
                Is45Plus = p.Age.HasValue && p.Age.Value >= 45 ? 1 : 0
            })
            .GroupBy(_ => 1)
            .Select(g => new
            {
                CompletedProfiles = g.Sum(p => p.IsCompleted),
                MaleCount = g.Sum(p => p.IsMale),
                FemaleCount = g.Sum(p => p.IsFemale),
                Friends = g.Sum(p => p.IsFriends),
                Relationship = g.Sum(p => p.IsRelationship),
                AdultOnly = g.Sum(p => p.IsAdultOnly),
                AgeUnder18 = g.Sum(p => p.IsUnder18),
                Age18To24 = g.Sum(p => p.Is18To24),
                Age25To34 = g.Sum(p => p.Is25To34),
                Age35To44 = g.Sum(p => p.Is35To44),
                Age45Plus = g.Sum(p => p.Is45Plus)
            })
            .FirstOrDefaultAsync(cancellationToken);

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
            userStats?.TotalUsers ?? 0,
            profileStats?.CompletedProfiles ?? 0,
            userStats?.BannedUsers ?? 0,
            profileStats?.MaleCount ?? 0,
            profileStats?.FemaleCount ?? 0,
            userStats?.NewUsers24h ?? 0,
            userStats?.NewUsers7d ?? 0,
            userStats?.NewUsers30d ?? 0,
            profileStats?.Friends ?? 0,
            profileStats?.Relationship ?? 0,
            profileStats?.AdultOnly ?? 0,
            profileStats?.AgeUnder18 ?? 0,
            profileStats?.Age18To24 ?? 0,
            profileStats?.Age25To34 ?? 0,
            profileStats?.Age35To44 ?? 0,
            profileStats?.Age45Plus ?? 0,
            topCities,
            topCountries
        );
    }

    public async Task<IReadOnlyList<AdminCityStatsDto>> GetTopCitiesStatsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        var rawStats = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => !string.IsNullOrEmpty(p.City))
            .Select(p => new
            {
                City = p.City!,
                Country = p.CityRef != null ? p.CityRef.Country : null,
                IsCompleted = p.IsCompleted ? 1 : 0,
                IsMale = p.Gender == Gender.Male ? 1 : 0,
                IsFemale = p.Gender == Gender.Female ? 1 : 0
            })
            .GroupBy(p => new { p.City, p.Country })
            .Select(g => new
            {
                g.Key.City,
                g.Key.Country,
                UserCount = g.Count(),
                CompletedCount = g.Sum(x => x.IsCompleted),
                MaleCount = g.Sum(x => x.IsMale),
                FemaleCount = g.Sum(x => x.IsFemale)
            })
            .OrderByDescending(x => x.UserCount)
            .ThenBy(x => x.City)
            .Take(count)
            .ToListAsync(cancellationToken);

        return rawStats.Select(s => new AdminCityStatsDto(
            s.City,
            s.Country,
            s.UserCount,
            s.CompletedCount,
            s.MaleCount,
            s.FemaleCount
        )).ToList();
    }

    public async Task<AdminCityStatsDto?> GetCityStatsAsync(string cityName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return null;

        var cleanCity = cityName.Trim().ToLower();

        var stats = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.City != null && (p.City.ToLower() == cleanCity || (p.CityRef != null && p.CityRef.Name.ToLower() == cleanCity)))
            .Select(p => new
            {
                City = p.City!,
                Country = p.CityRef != null ? p.CityRef.Country : null,
                IsCompleted = p.IsCompleted ? 1 : 0,
                IsMale = p.Gender == Gender.Male ? 1 : 0,
                IsFemale = p.Gender == Gender.Female ? 1 : 0
            })
            .GroupBy(_ => 1)
            .Select(g => new
            {
                DisplayName = g.Select(p => p.City).FirstOrDefault() ?? cityName,
                Country = g.Select(p => p.Country).FirstOrDefault(),
                UserCount = g.Count(),
                CompletedCount = g.Sum(x => x.IsCompleted),
                MaleCount = g.Sum(x => x.IsMale),
                FemaleCount = g.Sum(x => x.IsFemale)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stats == null || stats.UserCount == 0) return null;

        return new AdminCityStatsDto(
            stats.DisplayName,
            stats.Country,
            stats.UserCount,
            stats.CompletedCount,
            stats.MaleCount,
            stats.FemaleCount
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
