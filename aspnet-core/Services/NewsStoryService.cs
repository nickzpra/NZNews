using Microsoft.AspNetCore.Mvc;
using NZNewsApi.Dtos;
using NZNewsApi.Services.Interfaces;
using StackExchange.Redis;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NZNewsApi.Models;

namespace NZNewsApi.Services
{
    public class NewsStoryService : INewsStoryService
    {
        private readonly HttpClient _httpClient;
        private readonly ICacheService _redis;
        private readonly IConfiguration _configuration;

        private readonly string _getStoriesIdsUrl;

        public NewsStoryService(HttpClient httpClient, IConfiguration configuration, ICacheService redis)
        {
            _httpClient = httpClient;
            _redis = redis;
            _configuration = configuration;

            _getStoriesIdsUrl = _configuration.GetValue<string>("HackerNewsUrls:GetStoriesIdsUrl");
        }

        public async Task<int> GetTotalStoriesCount(string storyType = "new")
        {
            var url = _getStoriesIdsUrl.Replace("{storyType}", storyType);
            string cachedResult = await _redis.GetValueAsync(CacheKeys.TotalItemsCountCacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                return int.Parse(cachedResult);
            }

            var response = await _httpClient.GetStringAsync(url);
            int totalCount = JsonSerializer.Deserialize<List<int>>(response).Count;

            await _redis.SetValueAsync(CacheKeys.TotalItemsCountCacheKey, totalCount.ToString());
            return totalCount;
        }

        public async Task<PagedResultDto> Get(int page = 1, int pageSize = 10, string storyType = "new", string search = null)
        {
            await UpdateChangedItems(storyType);
            var itemIds = await GetItemIds(storyType);
            if (!string.IsNullOrEmpty(search))
            {
                return await SearchStories(itemIds, page, pageSize, search);
            }
            else
            {
                return await GetAllStories(itemIds, page, pageSize);
            }
        }

        public void FlushCache() => _redis.FlushCache();

        private async Task<List<int>> GetItemIds(string storyType)
        {
            var cachedResult = await _redis.GetValueAsync(CacheKeys.AllIdsCacheKey(storyType));
            if (!string.IsNullOrEmpty(cachedResult) && await IsCacheValid(CacheKeys.LastIdsUpdateCacheKey))
            {
                return JsonSerializer.Deserialize<List<int>>(cachedResult);
            }

            var response = await _httpClient.GetStringAsync(_getStoriesIdsUrl.Replace("{storyType}", storyType));
            var storyIds = JsonSerializer.Deserialize<List<int>>(response).OrderByDescending(x => x).ToList();

            await _redis.SetValueAsync(CacheKeys.AllIdsCacheKey(storyType), JsonSerializer.Serialize(storyIds));
            await _redis.SetValueAsync(CacheKeys.LastIdsUpdateCacheKey, DateTime.Now.ToString());

            return storyIds;
        }

        private async Task UpdateChangedItems(string storyType)
        {
            if (await ShouldDataBeRefreshed(storyType))
            {
                var response = await _httpClient.GetStringAsync(_configuration.GetValue<string>("HackerNewsUrls:GetUpdatesUrl"));
                var changedItemIds = JsonSerializer.Deserialize<List<int>>(JsonDocument.Parse(response).RootElement.GetProperty("items"));
                await ClearOutOfDateItemsFromCache(changedItemIds);
                await _redis.SetValueAsync(CacheKeys.LastUpdateChangedItemIdsCacheKey(storyType), DateTime.Now.ToString());
            }
        }

        private async Task<bool> ShouldDataBeRefreshed(string storyType)
        {
            var lastUpdateResult = await _redis.GetValueAsync(CacheKeys.LastUpdateChangedItemIdsCacheKey(storyType));
            if (!string.IsNullOrEmpty(lastUpdateResult) && DateTime.TryParse(lastUpdateResult, out DateTime lastUpdatedDate))
            {
                return (DateTime.Now - lastUpdatedDate).TotalMinutes > 5;
            }
            return true;
        }

        private async Task ClearOutOfDateItemsFromCache(List<int>? changedItemIds)
        {
            if (changedItemIds == null) return;

            foreach (var itemId in changedItemIds)
            {
                await _redis.DeleteValueAsync(CacheKeys.ItemCacheKey(itemId));
            }
        }

        private async Task<PagedResultDto> GetAllStories(List<int> itemIds, int page, int pageSize)
        {
            var stories = new List<StoryDto>();

            int totalStoriesCount = itemIds.Count;
            int totalPages = (totalStoriesCount + pageSize - 1) / pageSize;

            while (stories.Count < pageSize && page <= totalPages)
            {
                var paginatedStoryIds = itemIds.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                if (paginatedStoryIds.Count <= 0)
                {
                    break;
                }
                foreach (var id in paginatedStoryIds)
                {
                    // Calculate how many more stories can be added
                    int availableSpace = pageSize - stories.Count;

                    if (availableSpace > 0)
                    {
                        var item = await GetStory(id);
                        if (!string.IsNullOrEmpty(item.Url))
                        {
                            stories.Add(item);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                page++;
            }

            return new PagedResultDto { TotalCount = totalStoriesCount, Stories = stories.Take(pageSize).ToList() };
        }

        private async Task<PagedResultDto> SearchStories(List<int> itemIds, int page, int pageSize, string search)
        {
            var stories = new List<StoryDto>();
            var notFoundItemIds = await GetCachedNotFoundItemIds(search);
            var previousFoundItemIds = await GetCachedFoundItemIds(search);

            itemIds = FilterPreviouslySearchedIds(itemIds, notFoundItemIds, previousFoundItemIds);
            stories = await GetPreviouslyFoundItems(previousFoundItemIds, page, pageSize, search);

            int totalStoriesCount = itemIds.Count;// + previousFoundItemIds.Count;
            int totalPages = (totalStoriesCount + pageSize - 1) / pageSize;

            int searchPage = 1;

            while (stories.Count < pageSize + 1 && searchPage <= totalPages)
            {
                var paginatedStoryIds = itemIds.Skip((searchPage - 1) * pageSize).Take(pageSize).ToList();
                if (paginatedStoryIds.Count <= 0)
                {
                    break;
                }
                notFoundItemIds.AddRange(await AddStoriesToList(paginatedStoryIds, stories, search, pageSize));
                searchPage++;
            }

            //Add newly found stories Ids to previousFoundItemIds
            previousFoundItemIds.AddRange(stories.Select(x => x.Id).ToList());
            previousFoundItemIds = previousFoundItemIds.Distinct().ToList();

            await CacheSearchResults(search, previousFoundItemIds, notFoundItemIds);

            if (!string.IsNullOrEmpty(search))
            {
                totalStoriesCount = previousFoundItemIds.Count;
            }
            return new PagedResultDto { TotalCount = totalStoriesCount, Stories = stories.Take(pageSize).ToList() };
        }

        private async Task<List<int>> AddStoriesToList(List<int> paginatedStoryIds, List<StoryDto> stories, string search, int pageSize)
        {
            var notFoundItemIds = new List<int>();

            foreach (var id in paginatedStoryIds)
            {
                // Calculate how many more stories can be added
                int availableSpace = pageSize + 1 - stories.Count;

                if (availableSpace > 0)
                {
                    var item = await GetStory(id);
                    if (!string.IsNullOrEmpty(item.Url))
                    {
                        if (!string.IsNullOrEmpty(search))
                        {
                            if (item?.Title?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                stories.Add(item);
                            }
                            else
                            {
                                notFoundItemIds.Add(item.Id);
                            }
                        }
                        else
                        {
                            stories.Add(item);
                        }
                    }
                    else
                    {
                        notFoundItemIds.Add(item.Id);
                    }
                }
                else
                {
                    break;
                }
            }
            return notFoundItemIds;
        }

        private async Task CacheSearchResults(string searchCriteria, List<int> previousFoundItemIds, List<int> notFoundItemIds)
        {
            if (!string.IsNullOrEmpty(searchCriteria))
            {
                var serializedNotFoundIds = JsonSerializer.Serialize(notFoundItemIds.Distinct().OrderByDescending(x => x).ToList());
                await _redis.SetValueAsync($"NotFoundItems:{searchCriteria}", serializedNotFoundIds);

                var serializedPreviousFoundItemIds = JsonSerializer.Serialize(previousFoundItemIds.Distinct().OrderByDescending(x => x).ToList());                
                await _redis.SetValueAsync($"FoundItems:{searchCriteria}", serializedPreviousFoundItemIds);
            }
        }

        private async Task<List<int>> GetCachedNotFoundItemIds(string search)
        {
            var previousNotFoundItems = await _redis.GetValueAsync($"NotFoundItems:{search}");
            return string.IsNullOrEmpty(previousNotFoundItems) ? new List<int>() : JsonSerializer.Deserialize<List<int>>(previousNotFoundItems);
        }

        private async Task<List<int>> GetCachedFoundItemIds(string search)
        {
            var previousFoundItems = await _redis.GetValueAsync($"FoundItems:{search}");
            return string.IsNullOrEmpty(previousFoundItems) ? new List<int>() : JsonSerializer.Deserialize<List<int>>(previousFoundItems);
        }

        private async Task<List<StoryDto>> GetPreviouslyFoundItems(List<int> previousFoundItemIds, int searchPage, int pageSize, string search)
        {
            var stories = new List<StoryDto>();
            while (stories.Count < pageSize + 1)
            {
                var paginatedStoryIds = previousFoundItemIds.Skip((searchPage - 1) * pageSize).Take(pageSize).ToList();
                foreach (var id in paginatedStoryIds)
                {
                    // Calculate how many more stories can be added
                    int availableSpace = pageSize + 1 - stories.Count;

                    if (availableSpace > 0)
                    {
                        var item = await GetStory(id);
                        if (!string.IsNullOrEmpty(item.Url) && (string.IsNullOrEmpty(search) || item?.Title?.Contains(search, StringComparison.OrdinalIgnoreCase) == true))
                        {
                            stories.Add(item);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                if (!paginatedStoryIds.Any()) break;
                searchPage++;
            }
            return stories;
        }

        private async Task<StoryDto> GetStory(int itemId)
        {
            var cachedResult = await _redis.GetValueAsync(CacheKeys.ItemCacheKey(itemId));
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonSerializer.Deserialize<StoryDto>(cachedResult);
            }

            var storyUrl = _configuration.GetValue<string>("HackerNewsUrls:GetItemUrl").Replace("{itemId}", itemId.ToString());
            var storyResponse = await _httpClient.GetStringAsync(storyUrl);
            var item = JsonSerializer.Deserialize<StoryDto>(storyResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            //2. Store item to cache
            await _redis.SetValueAsync(CacheKeys.ItemCacheKey(itemId), JsonSerializer.Serialize(item));
            return item;
        }

        private List<int> FilterPreviouslySearchedIds(List<int> itemIds, List<int> previousNotFoundItemIds, List<int> previousFoundItemIds)
        {
            if (previousNotFoundItemIds != null)
            {
                itemIds = itemIds.Except(previousNotFoundItemIds).ToList();
            }

            if (previousFoundItemIds != null)
            {
                itemIds = itemIds.Except(previousFoundItemIds).ToList();
            }

            return itemIds;
        }

        private async Task<bool> IsCacheValid(string cacheKey)
        {
            var lastUpdateResult = await _redis.GetValueAsync(cacheKey);
            if (string.IsNullOrEmpty(lastUpdateResult)) return false;

            DateTime.TryParse(lastUpdateResult, out DateTime lastUpdatedDate);
            return (DateTime.Now - lastUpdatedDate).TotalMinutes <= 5;
        }
    }
}
