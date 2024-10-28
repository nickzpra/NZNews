namespace NZNewsApi.Dtos
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PagedResultDto
    {
        public int TotalCount { get; set; }  // Total number of items in the data set
        public List<StoryDto>? Stories { get; set; }  // Items for the current page
    }
}
