using NZNewsApi.Dtos;

namespace NZNewsApi.Services.Interfaces
{
    public interface INewsStoryService
    {
        Task<PagedResultDto> Get(int page, int pageSize, string storyType, string search);

        Task<int> GetTotalStoriesCount(string storyType);

        void FlushCache();

    }
}
