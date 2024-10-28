namespace NZNewsApi.Dtos
{
    public class StoryDto
    {
        public string? By { get; set; } = "";
        public int Descendants { get; set; }
        public int Id { get; set; }
        public int Score { get; set; }
        public long Time { get; set; }
        public string? Title { get; set; } = "";
        public string? Type { get; set; } = "";
        public string? Url { get; set; } = "";
    }
}
