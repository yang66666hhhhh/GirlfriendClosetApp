using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace ClosetApp.Infrastructure.Services;

public class WeatherService : IWeatherService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public WeatherService(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }

    public async Task<WeatherInfo?> GetCurrentWeatherAsync(string city)
    {
        var normalizedCity = city?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCity))
            return null;

        var cacheKey = $"weather:{normalizedCity.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out WeatherInfo? cached) && cached != null)
            return cached;

        try
        {
            var location = await SearchLocationAsync(normalizedCity);
            if (location == null)
                return null;

            var weather = await FetchWeatherAsync(location);
            if (weather == null)
                return null;

            _cache.Set(cacheKey, weather, TimeSpan.FromMinutes(15));
            return weather;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to fetch weather for city {City}", normalizedCity);
            return null;
        }
    }

    public int GetFallbackTemperature(DateTimeOffset? date = null)
    {
        var month = (date ?? DateTimeOffset.Now).Month;
        return month switch
        {
            12 or 1 or 2 => 8,
            3 or 4 or 11 => 16,
            5 or 10 => 22,
            _ => 28
        };
    }

    private async Task<WeatherLocation?> SearchLocationAsync(string city)
    {
        var uri =
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=zh&format=json";
        using var response = await _httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<GeocodingResponse>(stream, JsonOptions);
        var result = payload?.Results?.FirstOrDefault();
        if (result == null)
            return null;

        return new WeatherLocation(
            result.Name ?? city,
            result.Admin1,
            result.Country,
            result.Latitude,
            result.Longitude,
            result.Timezone);
    }

    private async Task<WeatherInfo?> FetchWeatherAsync(WeatherLocation location)
    {
        var latitude = location.Latitude.ToString(CultureInfo.InvariantCulture);
        var longitude = location.Longitude.ToString(CultureInfo.InvariantCulture);
        var uri =
            $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,weather_code&timezone=auto&forecast_days=1";

        using var response = await _httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<ForecastResponse>(stream, JsonOptions);
        if (payload?.Current == null)
            return null;

        return new WeatherInfo
        {
            City = BuildCityDisplayName(location),
            Temperature = (int)Math.Round(payload.Current.Temperature2M),
            Condition = MapWeatherCode(payload.Current.WeatherCode),
            Humidity = (int)Math.Round(payload.Current.RelativeHumidity2M),
            Timezone = payload.Timezone ?? location.Timezone ?? string.Empty,
            ObservedAt = ParseObservedAt(payload.Current.Time, payload.Timezone)
        };
    }

    private static DateTimeOffset? ParseObservedAt(string? time, string? timezone)
    {
        if (string.IsNullOrWhiteSpace(time))
            return null;

        if (DateTimeOffset.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var observedAt))
            return observedAt;

        if (DateTime.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var localTime))
        {
            try
            {
                var zone = !string.IsNullOrWhiteSpace(timezone)
                    ? TimeZoneInfo.FindSystemTimeZoneById(timezone)
                    : TimeZoneInfo.Local;
                var offset = zone.GetUtcOffset(localTime);
                return new DateTimeOffset(localTime, offset);
            }
            catch
            {
                return new DateTimeOffset(localTime);
            }
        }

        return null;
    }

    private static string BuildCityDisplayName(WeatherLocation location)
    {
        var parts = new[]
        {
            location.Name,
            location.Admin1,
            location.Country
        };

        return string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Distinct());
    }

    private static string MapWeatherCode(int code)
    {
        return code switch
        {
            0 => "晴",
            1 => "大部晴朗",
            2 => "局部多云",
            3 => "阴",
            45 or 48 => "雾",
            51 or 53 or 55 => "毛毛雨",
            56 or 57 => "冻毛毛雨",
            61 or 63 or 65 => "雨",
            66 or 67 => "冻雨",
            71 or 73 or 75 or 77 => "雪",
            80 or 81 or 82 => "阵雨",
            85 or 86 => "阵雪",
            95 => "雷暴",
            96 or 99 => "雷暴夹冰雹",
            _ => "天气未知"
        };
    }

    private sealed record WeatherLocation(
        string Name,
        string? Admin1,
        string? Country,
        double Latitude,
        double Longitude,
        string? Timezone);

    private sealed class GeocodingResponse
    {
        [JsonPropertyName("results")]
        public List<GeocodingResult>? Results { get; set; }
    }

    private sealed class GeocodingResult
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("admin1")]
        public string? Admin1 { get; set; }
    }

    private sealed class ForecastResponse
    {
        [JsonPropertyName("timezone")]
        public string? Timezone { get; set; }

        [JsonPropertyName("current")]
        public CurrentWeatherPayload? Current { get; set; }
    }

    private sealed class CurrentWeatherPayload
    {
        [JsonPropertyName("time")]
        public string? Time { get; set; }

        [JsonPropertyName("temperature_2m")]
        public double Temperature2M { get; set; }

        [JsonPropertyName("relative_humidity_2m")]
        public double RelativeHumidity2M { get; set; }

        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; set; }
    }
}
