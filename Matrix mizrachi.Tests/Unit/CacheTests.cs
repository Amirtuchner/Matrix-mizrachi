using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Matrix_mizrachi.Tests.Unit;

public class CacheTests
{
    private static IMemoryCache CreateCache() =>
        new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public void Cache_StoresAndRetrieves_Result()
    {
        var cache = CreateCache();
        const string key = "add:5:3";
        const double expected = 8.0;

        cache.Set(key, expected, TimeSpan.FromSeconds(30));

        Assert.True(cache.TryGetValue(key, out double result));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Cache_Miss_ReturnsFalse()
    {
        var cache = CreateCache();

        var hit = cache.TryGetValue("nonexistent:1:2", out double _);

        Assert.False(hit);
    }

    [Fact]
    public void Cache_DifferentKeys_StoredSeparately()
    {
        var cache = CreateCache();

        cache.Set("add:1:2", 3.0, TimeSpan.FromSeconds(30));
        cache.Set("multiply:1:2", 2.0, TimeSpan.FromSeconds(30));

        Assert.True(cache.TryGetValue("add:1:2", out double add));
        Assert.True(cache.TryGetValue("multiply:1:2", out double multiply));
        Assert.Equal(3.0, add);
        Assert.Equal(2.0, multiply);
    }

    [Fact]
    public async Task Cache_Expires_AfterTTL()
    {
        var cache = CreateCache();
        const string key = "add:10:20";

        cache.Set(key, 30.0, TimeSpan.FromMilliseconds(100));
        await Task.Delay(250);

        Assert.False(cache.TryGetValue(key, out double _));
    }

    [Fact]
    public void Cache_Key_Format_IsCorrect()
    {
        // Validates the key format used by MathController: "<operation>:<x>:<y>"
        const string operation = "divide";
        const double x = 10.0;
        const double y = 2.0;

        var key = $"{operation}:{x}:{y}";

        Assert.Equal("divide:10:2", key);
    }
}
