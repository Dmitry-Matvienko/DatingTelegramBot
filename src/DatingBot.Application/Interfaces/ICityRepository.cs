using DatingBot.Domain.Entities;

namespace DatingBot.Application.Interfaces;

public interface ICityRepository
{
    Task<City?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<City?> FindExactByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<City>> SearchSuggestionsAsync(string query, int limit = 5, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<City>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(City City, double DistanceKm)>> GetNearbyCitiesAsync(double latitude, double longitude, double maxRadiusKm, CancellationToken cancellationToken = default);
}
