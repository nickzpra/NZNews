using StackExchange.Redis;

namespace NZNewsApi.Models
{
    public static class CacheKeys
    {
        public static string GetChangedIdsCacheKey(string storyType)
        {
            return $"ChangedItemIds:{storyType}";
        }

        public static string AllIdsCacheKey(string storyType)
        {
            return $"ItemIds:{storyType}";
        }

        public static string LastUpdateChangedItemIdsCacheKey(string storyType)
        {
            return $"LastUpdateChangedItemIds:{storyType}";
        }
        public static string ItemCacheKey(int storyId)
        {
            return $"Item:{storyId}";
        }

        public const string LastIdsUpdateCacheKey = $"LastUpdateItemIds";

        public const string TotalItemsCountCacheKey = $"TotalStoriesCount";

    }
}
