using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using StackExchange.Redis;

namespace MiniCambio.Rates.IntegrationTests;

public class RatesCacheTests(RatesApiFactory factory)
{
    [Fact]
    public async Task GetRate_CalledTwice_ReturnsSameCachedRate()
    {
        var client = factory.CreateClient();

        var first = await GetUsdAsync(client);
        var second = await GetUsdAsync(client);

        Assert.Equal(first.GetProperty("quotedAt").GetString(), second.GetProperty("quotedAt").GetString());
        Assert.Equal(first.GetProperty("midRate").GetDecimal(), second.GetProperty("midRate").GetDecimal());
    }

    [Fact]
    public async Task GetRate_AfterCall_KeyIsStoredInRedisWithTtl()
    {
        await GetUsdAsync(factory.CreateClient());

        await using var redis = await ConnectionMultiplexer.ConnectAsync(factory.RedisConnectionString);
        var database = redis.GetDatabase();
        var value = await database.StringGetAsync("rates:live-mid:USD");
        var ttl = await database.KeyTimeToLiveAsync("rates:live-mid:USD");

        Assert.Equal(5.40m, JsonDocument.Parse(value.ToString()).RootElement.GetProperty("midRate").GetDecimal());
        Assert.NotNull(ttl);
        Assert.InRange(ttl.Value, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(300));
    }

    [Fact]
    public async Task GetRate_RedisUnavailable_StillReturnsRate()
    {
        // Porta 1: ninguém escuta. A API tem que subir e responder mesmo assim, só que sem cache.
        await using var withoutRedis = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Redis", "127.0.0.1:1"));
        var client = withoutRedis.CreateClient();

        var response = await client.GetAsync("/api/v1/rates/USD", TestContext.Current.CancellationToken);
        var ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    private static async Task<JsonElement> GetUsdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/rates/USD", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }
}
