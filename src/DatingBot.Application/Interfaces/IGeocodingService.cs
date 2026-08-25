using DatingBot.Domain.Enums;

namespace DatingBot.Application.Interfaces;

public record GeocodingLocation(string CityName, string? Region, string Country, double Latitude, double Longitude);

public interface IGeocodingService
{
    Task<GeocodingLocation?> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        AppLanguage language = AppLanguage.Russian,
        CancellationToken cancellationToken = default);
}
