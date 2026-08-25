using DatingBot.Application.Interfaces;
using DatingBot.Domain.Entities;
using DatingBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatingBot.Infrastructure.Repositories;

public class CityRepository(AppDbContext context) : ICityRepository
{
    private static readonly Dictionary<string, string> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["питер"] = "Санкт-Петербург",
        ["спб"] = "Санкт-Петербург",
        ["мск"] = "Москва",
        ["екб"] = "Екатеринбург",
        ["нск"] = "Новосибирск",
        ["ростов"] = "Ростов-на-Дону",
        ["нижний"] = "Нижний Новгород",
        ["владик"] = "Владивосток",
        ["киев"] = "Киев",
        ["київ"] = "Киев",
        ["харьков"] = "Харьков",
        ["харків"] = "Харьков",
        ["одесса"] = "Одесса",
        ["одеса"] = "Одесса",
        ["львов"] = "Львов",
        ["львів"] = "Львов",
        ["днепр"] = "Днепр",
        ["дніпро"] = "Днепр",
        ["днепропетровск"] = "Днепр"
    };

    public async Task<City?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await context.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<City?> FindExactByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var cleanName = name.Trim();
        if (Synonyms.TryGetValue(cleanName, out var canonicalName))
        {
            cleanName = canonicalName;
        }

        var result = await context.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => EF.Functions.Like(c.Name, cleanName), cancellationToken);

        if (result is not null) return result;

        // Попытка с нормализацией е/ё
        if (cleanName.Contains('ё') || cleanName.Contains('Ё'))
        {
            var replaced = cleanName.Replace('ё', 'е').Replace('Ё', 'Е');
            result = await context.Cities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => EF.Functions.Like(c.Name, replaced), cancellationToken);
        }

        return result;
    }

    public async Task<IReadOnlyList<City>> SearchSuggestionsAsync(string query, int limit = 5, CancellationToken cancellationToken = default)
    {
        var cleanQuery = query.Trim();
        if (Synonyms.TryGetValue(cleanQuery, out var canonicalName))
        {
            cleanQuery = canonicalName;
        }

        // 1. Быстрый поиск по префиксу в БД (использует индекс IX_Cities_Name)
        var prefixMatches = await context.Cities
            .AsNoTracking()
            .Where(c => c.Name.StartsWith(cleanQuery))
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (prefixMatches.Count >= limit)
        {
            return prefixMatches;
        }

        var results = new List<City>(prefixMatches);
        var seenIds = new HashSet<int>(prefixMatches.Select(c => c.Id));

        // 2. Поиск по подстроке в БД
        var remaining = limit - results.Count;
        var substringMatches = await context.Cities
            .AsNoTracking()
            .Where(c => c.Name.Contains(cleanQuery) && !seenIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Take(remaining)
            .ToListAsync(cancellationToken);

        results.AddRange(substringMatches);
        foreach (var m in substringMatches)
        {
            seenIds.Add(m.Id);
        }

        if (results.Count >= limit)
        {
            return results;
        }

        // 3. Нечеткий поиск при опечатках (Fuzzy search)
        if (cleanQuery.Length >= 3)
        {
            var queryLower = cleanQuery.ToLowerInvariant();
            var prefix2 = cleanQuery[..Math.Min(2, cleanQuery.Length)];
            var candidates = await context.Cities
                .AsNoTracking()
                .Where(c => c.Name.StartsWith(prefix2) && !seenIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Id)
                .Take(100)
                .ToListAsync(cancellationToken);

            if (candidates.Count < (limit - results.Count) && cleanQuery.Length >= 3)
            {
                var prefix1 = cleanQuery[..1];
                var candidateIds = new HashSet<int>(candidates.Select(c => c.Id).Concat(seenIds));
                var moreCandidates = await context.Cities
                    .AsNoTracking()
                    .Where(c => c.Name.StartsWith(prefix1) && !candidateIds.Contains(c.Id))
                    .OrderBy(c => c.Name)
                    .ThenBy(c => c.Id)
                    .Take(100)
                    .ToListAsync(cancellationToken);
                candidates.AddRange(moreCandidates);
            }

            if (candidates.Count > 0)
            {
                var fuzzyMatches = candidates
                    .Select(c => new
                    {
                        City = c,
                        Distance = CalculateLevenshteinDistance(c.Name.ToLowerInvariant(), queryLower)
                    })
                    .Where(x => x.Distance <= 2) // Допускаем до 2 опечаток
                    .OrderBy(x => x.Distance)
                    .Take(limit - results.Count)
                    .Select(x => x.City)
                    .ToList();

                results.AddRange(fuzzyMatches);
            }
        }

        return results;
    }

    public async Task<City> AddAsync(City city, CancellationToken cancellationToken = default)
    {
        context.Cities.Add(city);
        await context.SaveChangesAsync(cancellationToken);
        return city;
    }

    public async Task<IReadOnlyList<City>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.Cities.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(City City, double DistanceKm)>> GetNearbyCitiesAsync(double latitude, double longitude, double maxRadiusKm, CancellationToken cancellationToken = default)
    {
        // Географический Bounding Box для фильтрации в SQL перед расчетом точного расстояния
        const double kmPerLatDegree = 111.0;
        var deltaLat = maxRadiusKm / kmPerLatDegree;
        var cosLat = Math.Max(0.1, Math.Cos(DegreesToRadians(latitude)));
        var deltaLon = maxRadiusKm / (kmPerLatDegree * cosLat);

        var minLat = latitude - deltaLat;
        var maxLat = latitude + deltaLat;
        var minLon = longitude - deltaLon;
        var maxLon = longitude + deltaLon;

        // Фильтрация по индексу координат в SQL
        var bboxCandidates = await context.Cities
            .AsNoTracking()
            .Where(c => c.Latitude >= minLat && c.Latitude <= maxLat &&
                        c.Longitude >= minLon && c.Longitude <= maxLon)
            .ToListAsync(cancellationToken);

        return bboxCandidates
            .Select(c => (City: c, DistanceKm: CalculateHaversineDistance(latitude, longitude, c.Latitude, c.Longitude)))
            .Where(x => x.DistanceKm <= maxRadiusKm)
            .OrderBy(x => x.DistanceKm)
            .ToList();
    }

    public static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(earthRadiusKm * c, 1);
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);

    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        var n = source.Length;
        var m = target.Length;
        var matrix = new int[n + 1, m + 1];

        for (var i = 0; i <= n; matrix[i, 0] = i++) ;
        for (var j = 0; j <= m; matrix[0, j] = j++) ;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return matrix[n, m];
    }
}
