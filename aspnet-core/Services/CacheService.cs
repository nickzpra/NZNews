using StackExchange.Redis;
using NZNewsApi.Services.Interfaces;

namespace NZNewsApi.Services
{

    public class CacheService : ICacheService
    {
        private readonly IDatabase _db;
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public CacheService(IConnectionMultiplexer connectionMultiplexer)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _db = _connectionMultiplexer.GetDatabase();
        }

        public async Task SetValueAsync(string key, string value)
        {            
            await _db.StringSetAsync(key, value);
        }

        public async Task<string> GetValueAsync(string key)
        {            
            return await _db.StringGetAsync(key);
        }

        public async Task<RedisValue[]> GetHashKeysAsync(string key)
        {            
            return await _db.HashKeysAsync(key);
        }

        public async Task<bool> DeleteValueAsync(string key)
        {            
            return await _db.KeyDeleteAsync(key); ;
        }
        public async Task FlushCache()
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            
            // Get all keys and delete them
            foreach (var key in server.Keys())
            {
                await _db.KeyDeleteAsync(key);
            }
        }
    }
}
