using Newtonsoft.Json;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ray.YouTubeGrabber.Apis
{
    public interface IYouTubeApi
    {
        [Post("/youtubei/v1/browse")]
        Task<ApiResponse<ResponseOfVideos>> QueryVideos([Body(BodySerializationMethod.Serialized)] QueryDto body);
    }

    public class QueryDto
    {
        public QueryDto(string browseId, string paramsCode, string continuation = null)
        {
            this.browseId = browseId;
            this.paramsCode = paramsCode;
            this.continuation = continuation;
        }

        public Context context { get; set; } = new Context();

        public string browseId { get; set; }

        /// <summary>
        /// 控制访问用户主页的哪一个tab，首页、视频、短视频等
        /// </summary>
        //[AliasAs("params")]
        [JsonProperty(PropertyName = "params")]
        public string paramsCode { get; set; }

        /// <summary>
        /// 控制分页
        /// </summary>
        public string continuation { get; set; }
    }

    public class Context
    {
        public Client client { get; set; } = new Client();
    }

    public class Client
    {
        public string clientName { get; set; } = "WEB";
        public string clientVersion { get; set; } = "2.20230124.09.00";
    }


    public class ResponseOfVideos
    {
        public ResponseContents contents { get; set; }

        public List<OnResponseReceivedAction> onResponseReceivedActions { get; set; }
    }

    public class ResponseContents
    {
        public TwoColumnBrowseResultsRenderer twoColumnBrowseResultsRenderer { get; set; }
    }

    public class TwoColumnBrowseResultsRenderer
    {
        public List<Tab> tabs { get; set; }
    }

    public class Tab
    {
        public TabRenderer tabRenderer { get; set; }
    }

    public class TabRenderer
    {
        public string title { get; set; }

        public VideoContent content { get; set; }
    }

    public class VideoContent
    {
        public RichGridRenderer richGridRenderer { get; set; }

        public RichItemRenderer richItemRenderer { get; set; }

        public ContinuationItemRenderer continuationItemRenderer { get; set; }
    }

    public class RichGridRenderer
    {
        public List<VideoContent> contents { get; set; }
    }

    public class RichItemRenderer
    {
        public RichItemContent content { get; set; }
    }

    public class RichItemContent
    {
        public VideoRenderer videoRenderer { get; set; }
    }

    public class VideoRenderer
    {
        public string videoId { get; set; }

        public Title title { get; set; }

        public PublishedTimeText publishedTimeText { get; set; }
    }

    public class Title
    {
        public List<Run> runs { get; set; }
    }

    public class Run
    {
        public string text { get; set; }
    }

    public class ContinuationItemRenderer
    {
        public ContinuationEndpoint continuationEndpoint { get; set; }
    }

    public class ContinuationEndpoint
    {
        public ContinuationCommand continuationCommand { get; set; }
    }

    public class ContinuationCommand
    {
        public string token { get; set; }
    }

    public class PublishedTimeText
    {
        public string simpleText { get; set; }
    }


    public class OnResponseReceivedAction
    {
        public AppendContinuationItemsAction appendContinuationItemsAction { get; set; }
    }

    public class AppendContinuationItemsAction
    {
        public List<VideoContent> continuationItems { get; set; }
    }
}
