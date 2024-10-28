using NZNews.Api.tests;
using NZNewsApi.Dtos;
using NZNewsApi.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace NZNews.Api.Tests
{
    public class NZNewsControllerTest
    {
        private const int DefaultPageSize = 10;
        private readonly HttpClient _client;

        public NZNewsControllerTest()
        {
            var app = new NZNewsWebAppFactory();
            _client = app.CreateClient();
        }

        [Fact]
        public async Task Assert_Response_PageSize()
        {
            // Act
            var pagedResponse = await GetPagedNewsStories(pageSize: DefaultPageSize);

            // Assert
            Assert.Equal(DefaultPageSize, pagedResponse.Stories.Count);
        }

        [Fact]
        public async Task Assert_Pagination()
        {
            // Act - Page 1
            var pagedResponsePage1 = await GetPagedNewsStories(page: 1);
            Assert.NotNull(pagedResponsePage1);
            Assert.Equal(DefaultPageSize, pagedResponsePage1.Stories.Count);

            // Act - Page 2
            var pagedResponsePage2 = await GetPagedNewsStories(page: 2);
            Assert.NotNull(pagedResponsePage2);
            Assert.Equal(DefaultPageSize, pagedResponsePage2.Stories.Count);

            // Assert no duplicate IDs between pages
            var idsPage1 = pagedResponsePage1.Stories.Select(story => story.Id).ToHashSet();
            var idsPage2 = pagedResponsePage2.Stories.Select(story => story.Id).ToHashSet();
            Assert.Empty(idsPage1.Intersect(idsPage2));
        }

        [Fact]
        public async Task Assert_SearchParam()
        {
            // Act
            var search = "google";
            var pagedResponse = await GetPagedNewsStories(search: search);

            // Assert at least one title contains "te"
            Assert.NotNull(pagedResponse.Stories);
            Assert.True(pagedResponse.Stories.Any(story => story.Title.Contains("te", StringComparison.OrdinalIgnoreCase)));
        }

        [Fact]
        public async Task Assert_StoryType_Job()
        {
            // Act
            var storyType = "job";
            var pagedResponse = await GetPagedNewsStories(storyType: storyType);

            // Assert story type is "job"
            Assert.NotNull(pagedResponse.Stories);
            Assert.All(pagedResponse.Stories, story => Assert.Contains("job", story.Type, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<PagedResultDto> GetPagedNewsStories(
            int page = 1,
            int pageSize = DefaultPageSize,
            int? lastItemId = null,
            bool previousPage = false,
            string storyType = "new",
            string search = "")
        {
            var queryString = $"?&page={page}&pageSize={pageSize}&storyType={Uri.EscapeDataString(storyType)}&search={Uri.EscapeDataString(search)}";
            var response = await _client.GetAsync($"/api/news{queryString}");

            response.EnsureSuccessStatusCode();

            var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResultDto>();
            Assert.NotNull(pagedResponse);

            return pagedResponse;
        }
    }
}
