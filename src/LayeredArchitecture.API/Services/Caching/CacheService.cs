using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LayeredArchitecture.API.Telemetry;
using System.Diagnostics.Metrics;

namespace LayeredArchitecture.API.Services.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        _logger.LogDebug("Attempting to get cached value for key: {CacheKey}", key);
        
        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            ApiMetrics.CacheHits.Add(1, new KeyValuePair<string, object?>("cache.key", key));
            return Task.FromResult<T?>(value);
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", key);
        ApiMetrics.CacheMisses.Add(1, new KeyValuePair<string, object?>("cache.key", key));
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        _logger.LogDebug("Setting cache value for key: {CacheKey}", key);
        
        var options = new MemoryCacheEntryOptions
        {
            Size = 1,
            SlidingExpiration = expiration ?? TimeSpan.FromMinutes(15),
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
        };

        _memoryCache.Set(key, value, options);
        _logger.LogInformation("Cache value set for key: {CacheKey} with expiration: {Expiration}", 
            key, expiration ?? TimeSpan.FromMinutes(15));
        
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _logger.LogInformation("Removing cache value for key: {CacheKey}", key);
        _memoryCache.Remove(key);
        ApiMetrics.CacheEvictions.Add(1, new KeyValuePair<string, object?>("cache.key", key));
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        _logger.LogInformation("Removing cache values with prefix: {CachePrefix}", prefix);
        
        // Note: IMemoryCache doesn't have built-in support for removing by prefix
        // This is a simplified implementation. In a real application, you might want to
        // maintain a separate index of keys or use a more sophisticated caching solution.
        
        // For now, we'll log the operation and implement a more robust solution later
        _logger.LogWarning("RemoveByPrefixAsync not fully implemented for prefix: {CachePrefix}", prefix);
        
        return Task.CompletedTask;
    }
}