namespace Ray.YouTubeGrabber.Entities
{
    public class VideoIdTagEntity
    {
        public VideoIdTagEntity(string videoId)
        {
            IdTag = videoId;
        }

        public string IdTag { get; set; }
    }
}
