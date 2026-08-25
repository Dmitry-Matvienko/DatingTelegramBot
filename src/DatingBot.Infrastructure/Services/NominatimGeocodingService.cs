using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DatingBot.Application.Interfaces;
using DatingBot.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DatingBot.Infrastructure.Services;

public class NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger) : IGeocodingService
{
    public async Task<GeocodingLocation?> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        AppLanguage language = AppLanguage.Russian,
        CancellationToken cancellationToken = default)
    {
        var latStr = latitude.ToString("F6", CultureInfo.InvariantCulture);
        var lonStr = longitude.ToString("F6", CultureInfo.InvariantCulture);
        var langCode = GetLanguageCode(language);

        // 1. Попытка через OpenStreetMap Nominatim
        try
        {
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={latStr}&lon={lonStr}&format=json&zoom=12&addressdetails=1&accept-language={langCode}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("DatingTelegramBot/1.0 (contact: admin@datingbot.local)");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<NominatimResponse>(cancellationToken: cancellationToken);
                if (json?.Address is not null)
                {
                    var cityName = json.Address.City
                        ?? json.Address.Town
                        ?? json.Address.Village
                        ?? json.Address.Municipality
                        ?? json.Address.Hamlet
                        ?? json.Address.Suburb
                        ?? json.Address.County
                        ?? json.Address.State
                        ?? json.Name;

                    if (!string.IsNullOrWhiteSpace(cityName))
                    {
                        var region = json.Address.State ?? json.Address.StateDistrict ?? json.Address.Region;
                        var country = json.Address.Country ?? "Россия";
                        return new GeocodingLocation(cityName.Trim(), region?.Trim(), country.Trim(), latitude, longitude);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при геокодировании через Nominatim для ({Latitude}, {Longitude})", latitude, longitude);
        }

        // 2. Резервный геокодер: BigDataCloud Reverse Geocoding API
        try
        {
            var bdcUrl = $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={latStr}&longitude={lonStr}&localityLanguage={langCode}";
            var bdcResponse = await httpClient.GetFromJsonAsync<BigDataCloudResponse>(bdcUrl, cancellationToken);
            if (bdcResponse is not null)
            {
                var cityName = !string.IsNullOrWhiteSpace(bdcResponse.City)
                    ? bdcResponse.City
                    : (!string.IsNullOrWhiteSpace(bdcResponse.Locality) ? bdcResponse.Locality : bdcResponse.PrincipalSubdivision);

                if (!string.IsNullOrWhiteSpace(cityName))
                {
                    var region = bdcResponse.PrincipalSubdivision;
                    var country = bdcResponse.CountryName ?? "Россия";
                    return new GeocodingLocation(cityName.Trim(), region?.Trim(), country.Trim(), latitude, longitude);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при геокодировании через BigDataCloud для ({Latitude}, {Longitude})", latitude, longitude);
        }

        return null;
    }

    private static string GetLanguageCode(AppLanguage language) => language switch
    {
        AppLanguage.Russian => "ru",
        AppLanguage.Ukrainian => "uk",
        AppLanguage.English => "en",
        AppLanguage.Hindi => "hi",
        AppLanguage.Portuguese => "pt",
        AppLanguage.Indonesian => "id",
        _ => "ru"
    };

    private sealed class NominatimResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    private sealed class NominatimAddress
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("town")]
        public string? Town { get; set; }

        [JsonPropertyName("village")]
        public string? Village { get; set; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }

        [JsonPropertyName("hamlet")]
        public string? Hamlet { get; set; }

        [JsonPropertyName("suburb")]
        public string? Suburb { get; set; }

        [JsonPropertyName("county")]
        public string? County { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("state_district")]
        public string? StateDistrict { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }

    private sealed class BigDataCloudResponse
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("locality")]
        public string? Locality { get; set; }

        [JsonPropertyName("principalSubdivision")]
        public string? PrincipalSubdivision { get; set; }

        [JsonPropertyName("countryName")]
        public string? CountryName { get; set; }
    }
}
