using Contracts;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;

namespace IS.ImageService.Api.Services.CacheService
{
    public class Cacher : ICacher
    {
        private readonly IDistributedCache _distributedCache;
        public Cacher(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<string> GetChacheValueAsync(string key, CancellationToken ct)
        {
            string? cacheMember = await _distributedCache.GetStringAsync(key, ct);
            if(string.IsNullOrEmpty(cacheMember))
            {
                throw new Exception("Cache is empty");
            }

            return cacheMember;
        }

        public async Task SetCacheValueAsync(string key, RedisJobsCommon value, CancellationToken ct)
        {
            string? cacheMember = await _distributedCache.GetStringAsync(key!.ToString()!, ct);
            if (string.IsNullOrEmpty(cacheMember))
            {
                await _distributedCache.SetStringAsync(key, value!.ToString()!, ct);
            }
            else
            {
                var ifParsed = Enum.TryParse<RedisJobsCommon>(value.ToString(), out var parsedValue);
                cacheMember = ifParsed && (int)parsedValue > (int)value ? parsedValue.ToString() : value.ToString();

                await _distributedCache.SetStringAsync(key, cacheMember, ct);
            }
        }
    }
}
