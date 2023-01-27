namespace Ray.YouTubeGrabber.Entities
{
    public class VideoInfoEntity
    {
        public VideoInfoEntity(string id, string title, string createTime)
        {
            Id = id;
            Title = title;
            CreateTime = createTime;
        }

        public string Id { get; set; }

        public string Title { get; set; }

        public string CreateTime { get; set; }

        public string PlayUrl => $"https://www.youtube.com/watch?v={Id}";
    }
}
