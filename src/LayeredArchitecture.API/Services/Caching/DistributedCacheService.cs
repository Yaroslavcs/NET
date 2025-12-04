using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LayeredArchitecture.API.Services.Caching
{
    public class DistributedCacheService : IDistributedCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<DistributedCacheService> _logger;
        private readonly ApiMetrics _metrics;
        private readonly JsonSerializerOptions _jsonOptions;

        public DistributedCacheService(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            ILogger<DistributedCacheService> logger,
            ApiMetrics metrics)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            var activity = ApiActivitySource.Source.StartActivity("DistributedCache.Get");
            
            try
            {
                // Try L1 cache first (memory)
                if (_memoryCache.TryGetValue(key, out T? memoryValue))
                {
                    _logger.LogDebug("L1 cache hit for key: {Key}", key);
                    _metrics.RecordCacheHit("memory");
                    activity?.SetTag("cache.level", "L1");
                    activity?.SetTag("cache.hit", true);
                    return memoryValue;
                }

                _logger.LogDebug("L1 cache miss for key: {Key}", key);

                // Try L2 cache (Redis)
                var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
                if (!string.IsNullOrEmpty(distributedValue))
                {
                    var value = JsonSerializer.Deserialize<T>(distributedValue, _jsonOptions);
                    if (value != null)
                    {
                        // Populate L1 cache
                        _memoryCache.Set(key, value, TimeSpan.FromMinutes(5)); // Shorter TTL for L1
                        _logger.LogDebug("L2 cache hit for key: {Key}, populated L1 cache", key);
                        _metrics.RecordCacheHit("distributed");
                        activity?.SetTag("cache.level", "L2");
                        activity?.SetTag("cache.hit", true);
                        return value;
                    }
                }

                _logger.LogDebug("L2 cache miss for key: {Key}", key);
                _metrics.RecordCacheMiss("distributed");
                activity?.SetTag("cache.hit", false);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting value from distributed cache for key: {Key}", key);
                activity?.SetTag("cache.error", ex.Message);
                return null;
            }
            finally
            {
                activity?.Dispose();
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? memoryCacheExpiration = null, TimeSpan? distributedCacheExpiration = null, CancellationToken cancellationToken = default) where T : class
        {
            var activity = ApiActivitySource.Source.StartActivity("DistributedCache.Set");
            
            try
            {
                var memoryExpiration = memoryCacheExpiration ?? TimeSpan.FromMinutes(5);
                var distributedExpiration = distributedCacheExpiration ?? TimeSpan.FromMinutes(30);

                // Set L1 cache (memory)
                _memoryCache.Set(key, value, memoryExpiration);
                _logger.LogDebug("Set L1 cache for key: {Key} with expiration: {Expiration}", key, memoryExpiration);

                // Set L2 cache (Redis)
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                await _distributedCache.SetStringAsync(key, serializedValue, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = distributedExpiration
                }, cancellationToken);

                _logger.LogDebug("Set L2 cache for key: {Key} with expiration: {Expiration}", key, distributedExpiration);
                _metrics.RecordCacheSet("distributed");
                
                activity?.SetTag("cache.level", "L1+L2");
                activity?.SetTag("cache.key", key);
                activity?.SetTag("cache.memory_ttl", memoryExpiration.TotalSeconds);
                activity?.SetTag("cache.distributed_ttl", distributedExpiration.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value in distributed cache for key: {Key}", key);
                activity?.SetTag("cache.error", ex.Message);
                throw;
            }
            finally
            {
                activity?.Dispose();
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            var activity = ApiActivitySource.Source.StartActivity("DistributedCache.Remove");
            
            try
            {
                // Remove from both L1 and L2
                _memoryCache.Remove(key);
                await _distributedCache.RemoveAsync(key, cancellationToken);
                
                _logger.LogDebug("Removed cache for key: {Key}", key);
                activity?.SetTag("cache.key", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
                activity?.SetTag("cache.error", ex.Message);
                throw;
            }
            finally
            {
                activity?.Dispose();
            }
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            // Note: Redis doesn't support removing by prefix directly in IDistributedCache
            // This is a limitation of the interface. In production, you might want to
            // use StackExchange.Redis directly for more advanced operations.
            _logger.LogWarning("RemoveByPrefixAsync not fully implemented. Prefix: {Prefix}", prefix);
            
            // For now, we'll just log the attempt
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            // Check L1 first, then L2
            if (_memoryCache.TryGetValue(key, out _))
            {
                return true;
            }

            var value = await _distributedCache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(value);
        }
    }
}