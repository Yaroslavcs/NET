using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LayeredArchitecture.API.Services.Caching
{
    public interface ICacheWarmupService
    {
        Task WarmupCacheAsync(CancellationToken cancellationToken = default);
        Task RefreshCacheAsync(string cacheType, CancellationToken cancellationToken = default);
    }

    public class CacheWarmupService : ICacheWarmupService, IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheWarmupService> _logger;
        private readonly List<string> _warmupKeys = new();

        public CacheWarmupService(
            IServiceProvider serviceProvider,
            ILogger<CacheWarmupService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task WarmupCacheAsync(CancellationToken cancellationToken = default)
        {
            var activity = ApiActivitySource.Source.StartActivity("CacheWarmup");
            
            try
            {
                _logger.LogInformation("Starting cache warmup process");
                
                using var scope = _serviceProvider.CreateScope();
                var cacheService = scope.ServiceProvider.GetRequiredService<IDistributedCacheService>();
                
                // Define critical data to warmup
                var warmupTasks = new List<Task>
                {
                    WarmupReferenceDataAsync(cacheService, cancellationToken),
                    WarmupFrequentlyAccessedDataAsync(cacheService, cancellationToken),
                    WarmupConfigurationDataAsync(cacheService, cancellationToken)
                };

                await Task.WhenAll(warmupTasks);
                
                _logger.LogInformation("Cache warmup completed successfully. Warmed up {Count} cache entries", _warmupKeys.Count);
                activity?.SetTag("cache.warmup.count", _warmupKeys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache warmup");
                activity?.SetTag("cache.warmup.error", ex.Message);
                throw;
            }
            finally
            {
                activity?.Dispose();
            }
        }

        private async Task WarmupReferenceDataAsync(IDistributedCacheService cacheService, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Warming up reference data cache");
            
            // Example: Cache product categories, customer segments, etc.
            var referenceData = new Dictionary<string, object>
            {
                ["product_categories"] = new[] { "Electronics", "Clothing", "Books", "Home" },
                ["order_statuses"] = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" },
                ["customer_segments"] = new[] { "Regular", "Premium", "VIP" }
            };

            foreach (var (key, value) in referenceData)
            {
                var cacheKey = $"reference_{key}";
                await cacheService.SetAsync(cacheKey, value, TimeSpan.FromHours(24), TimeSpan.FromDays(7), cancellationToken);
                _warmupKeys.Add(cacheKey);
            }
            
            _logger.LogDebug("Reference data cache warmup completed");
        }

        private async Task WarmupFrequentlyAccessedDataAsync(IDistributedCacheService cacheService, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Warming up frequently accessed data cache");
            
            // This would typically involve database queries to get actual data
            // For now, we'll simulate with placeholder data
            var frequentlyAccessedData = new Dictionary<string, object>
            {
                ["popular_products"] = new[] { "prod_123", "prod_456", "prod_789" },
                ["featured_products"] = new[] { "prod_111", "prod_222", "prod_333" },
                ["system_settings"] = new { EnableCaching = true, MaxRetries = 3, TimeoutSeconds = 30 }
            };

            foreach (var (key, value) in frequentlyAccessedData)
            {
                var cacheKey = $"frequent_{key}";
                await cacheService.SetAsync(cacheKey, value, TimeSpan.FromMinutes(15), TimeSpan.FromHours(1), cancellationToken);
                _warmupKeys.Add(cacheKey);
            }
            
            _logger.LogDebug("Frequently accessed data cache warmup completed");
        }

        private async Task WarmupConfigurationDataAsync(IDistributedCacheService cacheService, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Warming up configuration data cache");
            
            var configurationData = new Dictionary<string, object>
            {
                ["api_endpoints"] = new { Products = "/api/products", Orders = "/api/orders", Customers = "/api/customers" },
                ["feature_flags"] = new { NewUI = true, BetaFeatures = false, Analytics = true }
            };

            foreach (var (key, value) in configurationData)
            {
                var cacheKey = $"config_{key}";
                await cacheService.SetAsync(cacheKey, value, TimeSpan.FromMinutes(30), TimeSpan.FromHours(6), cancellationToken);
                _warmupKeys.Add(cacheKey);
            }
            
            _logger.LogDebug("Configuration data cache warmup completed");
        }

        public async Task RefreshCacheAsync(string cacheType, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Refreshing {CacheType} cache", cacheType);
            
            // Remove existing cache entries for the specified type
            var keysToRefresh = _warmupKeys.Where(key => key.StartsWith(cacheType)).ToList();
            
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<IDistributedCacheService>();
            
            foreach (var key in keysToRefresh)
            {
                await cacheService.RemoveAsync(key, cancellationToken);
            }
            
            // Re-warmup the cache based on type
            switch (cacheType)
            {
                case "reference":
                    await WarmupReferenceDataAsync(cacheService, cancellationToken);
                    break;
                case "frequent":
                    await WarmupFrequentlyAccessedDataAsync(cacheService, cancellationToken);
                    break;
                case "config":
                    await WarmupConfigurationDataAsync(cacheService, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unknown cache type: {CacheType}", cacheType);
                    break;
            }
            
            _logger.LogInformation("Cache refresh completed for {CacheType}", cacheType);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Perform cache warmup on startup
                await WarmupCacheAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to warmup cache during startup");
                // Don't throw - we don't want to prevent the app from starting
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cache warmup service stopping");
            await Task.CompletedTask;
        }
    }
}