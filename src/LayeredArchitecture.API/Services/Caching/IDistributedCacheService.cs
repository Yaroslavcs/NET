using System;
using System.Threading;
using System.Threading.Tasks;

namespace LayeredArchitecture.API.Services.Caching
{
    public interface IDistributedCacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? memoryCacheExpiration = null, TimeSpan? distributedCacheExpiration = null, CancellationToken cancellationToken = default) where T : class;
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}