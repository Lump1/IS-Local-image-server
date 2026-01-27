using Contracts;
using Microsoft.Extensions.Caching.Distributed;

namespace IS.ImageService.Api.Services.CacheService
{
    public interface ICacher
    {
        Task<string> GetChacheValueAsync(string key, CancellationToken ct);
        Task SetCacheValueAsync(string key, RedisJobsCommon value, CancellationToken ct);
    }
}
