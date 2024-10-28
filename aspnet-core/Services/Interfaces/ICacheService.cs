using StackExchange.Redis;

namespace NZNewsApi.Services.Interfaces
{

    public interface ICacheService
    {
        Task SetValueAsync(string key, string value);
        Task<string> GetValueAsync(string key);
        Task<RedisValue[]> GetHashKeysAsync(string key);
        Task<bool> DeleteValueAsync(string key);
        Task FlushCache();
    }

}
