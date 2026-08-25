using System.Net;
using DatingBot.Domain.Enums;
using DatingBot.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DatingBot.UnitTests;

public class NominatimGeocodingServiceTests
{
    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handlerFunc(request));
        }
    }

    [Fact]
    public async Task ReverseGeocodeAsync_ShouldParseNominatimResponseCorrectly()
    {
        // Arrange
        const string json = """
        {
            "name": "Москва",
            "address": {
                "city": "Москва",
                "state": "Москва",
                "country": "Россия"
            }
        }
        """;

        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler);
        var service = new NominatimGeocodingService(httpClient, NullLogger<NominatimGeocodingService>.Instance);

        // Act
        var result = await service.ReverseGeocodeAsync(55.7558, 37.6173, AppLanguage.Russian);

        // Assert
        result.Should().NotBeNull();
        result!.CityName.Should().Be("Москва");
        result.Country.Should().Be("Россия");
    }

    [Fact]
    public async Task ReverseGeocodeAsync_ShouldFallbackToTownOrVillageWhenCityIsNull()
    {
        // Arrange
        const string json = """
        {
            "name": "Красногорск",
            "address": {
                "town": "Красногорск",
                "state": "Московская область",
                "country": "Россия"
            }
        }
        """;

        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler);
        var service = new NominatimGeocodingService(httpClient, NullLogger<NominatimGeocodingService>.Instance);

        // Act
        var result = await service.ReverseGeocodeAsync(55.8306, 37.3303, AppLanguage.Russian);

        // Assert
        result.Should().NotBeNull();
        result!.CityName.Should().Be("Красногорск");
        result.Region.Should().Be("Московская область");
    }

    [Fact]
    public async Task ReverseGeocodeAsync_ShouldFallbackToBigDataCloudWhenNominatimFails()
    {
        // Arrange: Nominatim 500 -> BDC 200
        const string bdcJson = """
        {
            "city": "Серпухов",
            "principalSubdivision": "Московская область",
            "countryName": "Россия"
        }
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.RequestUri!.Host.Contains("nominatim"))
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(bdcJson, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var service = new NominatimGeocodingService(httpClient, NullLogger<NominatimGeocodingService>.Instance);

        // Act
        var result = await service.ReverseGeocodeAsync(54.9167, 37.4167, AppLanguage.Russian);

        // Assert
        result.Should().NotBeNull();
        result!.CityName.Should().Be("Серпухов");
    }

    [Fact]
    public async Task ReverseGeocodeAsync_ShouldReturnNullWhenBothProvidersFail()
    {
        // Arrange: Both return 500
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler);
        var service = new NominatimGeocodingService(httpClient, NullLogger<NominatimGeocodingService>.Instance);

        // Act
        var result = await service.ReverseGeocodeAsync(0, 0, AppLanguage.Russian);

        // Assert
        result.Should().BeNull();
    }
}
