using System.Net;
using System.Net.Http;
using System.Text;
using ClosetApp.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace ClosetApp.Tests;

public class WeatherServiceTests
{
    [Fact]
    public async Task GetCurrentWeatherAsync_MapsOpenMeteoPayload()
    {
        var handler = new SequenceHttpMessageHandler(
            JsonResponse("""
                {
                  "results": [
                    {
                      "name": "Shanghai",
                      "latitude": 31.2222,
                      "longitude": 121.4581,
                      "timezone": "Asia/Shanghai",
                      "country": "China",
                      "admin1": "Shanghai"
                    }
                  ]
                }
                """),
            JsonResponse("""
                {
                  "timezone": "Asia/Shanghai",
                  "current": {
                    "time": "2026-05-20T16:00",
                    "temperature_2m": 26.4,
                    "relative_humidity_2m": 68,
                    "weather_code": 1
                  }
                }
                """));

        var service = CreateService(handler);

        var weather = await service.GetCurrentWeatherAsync("Shanghai");

        Assert.NotNull(weather);
        Assert.Equal("Shanghai · China", weather!.City);
        Assert.Equal(26, weather.Temperature);
        Assert.Equal("大部晴朗", weather.Condition);
        Assert.Equal(68, weather.Humidity);
        Assert.Equal("Asia/Shanghai", weather.Timezone);
        Assert.True(weather.ObservedAt.HasValue);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_UsesCacheForSameCity()
    {
        var handler = new SequenceHttpMessageHandler(
            JsonResponse("""
                {
                  "results": [
                    {
                      "name": "Hangzhou",
                      "latitude": 30.2741,
                      "longitude": 120.1551,
                      "timezone": "Asia/Shanghai",
                      "country": "China",
                      "admin1": "Zhejiang"
                    }
                  ]
                }
                """),
            JsonResponse("""
                {
                  "timezone": "Asia/Shanghai",
                  "current": {
                    "time": "2026-05-20T17:00",
                    "temperature_2m": 24.2,
                    "relative_humidity_2m": 70,
                    "weather_code": 3
                  }
                }
                """));

        var service = CreateService(handler);

        var first = await service.GetCurrentWeatherAsync("Hangzhou");
        var second = await service.GetCurrentWeatherAsync("Hangzhou");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(2, handler.CallCount);
    }

    private static WeatherService CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new WeatherService(client, cache);
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class SequenceHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_responses.Count == 0)
                throw new InvalidOperationException("No more fake responses configured.");

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
