using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Ray.YouTubeGrabber.Apis;
using Ray.YouTubeGrabber.Entities;
using Volo.Abp.DependencyInjection;

namespace Ray.YouTubeGrabber;

public class HelloWorldService : ITransientDependency
{
    private readonly IYouTubeApi _youtubeApi;
    private readonly GrabberOptions _grabberOptions;
    public ILogger<HelloWorldService> Logger { get; set; }

    public HelloWorldService(IYouTubeApi youtubeApi, IOptions<GrabberOptions> grabberOptions)
    {
        _youtubeApi = youtubeApi;
        _grabberOptions = grabberOptions.Value;
        Logger = NullLogger<HelloWorldService>.Instance;
    }

    public async Task SayHelloAsync()
    {
        Logger.LogInformation("Hello World!");

        List<VideoInfoEntity> videoInfoList = new List<VideoInfoEntity>();

        //第一页，每页30条
        Logger.LogInformation("开始查询第一页");
        var re = await _youtubeApi.QueryVideos(new QueryDto(_grabberOptions.BrowseId, _grabberOptions.ParamsCode));
        var videoContents = re.Content.contents.twoColumnBrowseResultsRenderer.tabs[1]
            .tabRenderer.content.richGridRenderer.contents;

        //获取tag
        var tags = GetVideoIdTagEntities().Select(x => x.IdTag).ToList();

        videoInfoList.AddRange(MapVideoInfoEntityList(videoContents, tags, out bool mapTag));
        Logger.LogInformation("共{count}条", videoInfoList.Count);

        if (mapTag)
        {
            //add
            InsertEntities(videoInfoList);
            //update tag
            InsertAndDeleteVideoIdTags(videoInfoList.Take(3).Select(x => x.Id).ToList());
            return;
        }

        //根据continuation拿下一页
        var continuation = GetContinuation(videoContents);
        Logger.LogInformation("continuation:{continuation}", continuation);
        var currentPage = 1;
        while (!continuation.IsNullOrWhiteSpace())
        {
            currentPage++;
            Logger.LogInformation("查询第{page}页", currentPage);

            var next = await _youtubeApi.QueryVideos(new QueryDto(null, null, continuation));
            var videoContentList = next.Content.onResponseReceivedActions.FirstOrDefault()
                ?.appendContinuationItemsAction.continuationItems;

            videoInfoList.AddRange(MapVideoInfoEntityList(videoContentList, tags, out bool map));
            Logger.LogInformation("共{count}条", videoInfoList.Count);

            if (map)
            {
                //add
                InsertEntities(videoInfoList);
                //update tag
                InsertAndDeleteVideoIdTags(videoInfoList.Take(3).Select(x => x.Id).ToList());
                break;
            }

            continuation = GetContinuation(videoContentList);
            await Task.Delay(5 * 1000);
        }

        SaveEntities(videoInfoList);
        InsertAndDeleteVideoIdTags(videoInfoList.Take(3).Select(x => x.Id).ToList());
        Logger.LogInformation("All Done");
    }

    private string GetContinuation(List<VideoContent> videoContents)
    {
        try
        {
            var continuation = videoContents.LastOrDefault();

            return continuation?.continuationItemRenderer?.continuationEndpoint.continuationCommand.token;
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return null;
        }
    }

    private List<VideoInfoEntity> MapVideoInfoEntityList(List<VideoContent> videoContents, List<string> tags, out bool mapTag)
    {
        var re = new List<VideoInfoEntity>();
        mapTag = false;

        if (videoContents == null) return re;

        foreach (var videoContent in videoContents)
        {
            if (videoContent.continuationItemRenderer != null) break;

            var videoInfo = videoContent.richItemRenderer.content.videoRenderer;

            if (tags.Contains(videoInfo.videoId))
            {
                mapTag = true;
                break;
            }

            var videoEntity = new VideoInfoEntity(videoInfo.videoId,
                videoInfo.title.runs.FirstOrDefault()?.text,
                videoInfo.publishedTimeText.simpleText);
            re.Add(videoEntity);
        }

        return re;
    }

    private void SaveEntities(List<VideoInfoEntity> list)
    {
        Logger.LogInformation("新增{count}条", list.Count);
        var contents = new List<string>
        {
            "Id,Title,Create Time,Play Url"
        };

        foreach (var videoInfoEntity in list)
        {
            contents.Add($"{videoInfoEntity.Id},{ConvertStrForCsv(videoInfoEntity.Title)},{videoInfoEntity.CreateTime},{videoInfoEntity.PlayUrl}");
        }

        if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");
        File.WriteAllLines("Data/data.csv", contents);
    }

    private List<string> GetCsvStrList()
    {
        List<string> list = new List<string>();
        var filePath = "Data/data.csv";

        if (!File.Exists(filePath)) return list;

        var contentList = File.ReadAllLines(filePath).ToList();
        contentList.RemoveAll(x => x.IsNullOrWhiteSpace());

        return contentList;
    }

    private void InsertEntities(List<VideoInfoEntity> addEntityList)
    {
        var addContentList = new List<string>();

        foreach (var videoInfoEntity in addEntityList)
        {
            addContentList.Add($"{videoInfoEntity.Id},{ConvertStrForCsv(videoInfoEntity.Title)},{videoInfoEntity.CreateTime},{videoInfoEntity.PlayUrl}");
        }

        //获取已有
        var existList = GetCsvStrList();

        Logger.LogInformation("已存在{count}条", existList.Count - 1);
        Logger.LogInformation("新增{count}条", addEntityList.Count);

        existList.InsertRange(1, addContentList);
        File.WriteAllLines("Data/data.csv", existList);
    }

    private string ConvertStrForCsv(string source)
    {
        //如果存在英文逗号，则使用双引包裹
        source = source.Replace(",", "\",\"");

        //如果存在英文引号，则变为double
        source = source.Replace("\"", "\"\"");

        //最后在最外层使用双引包裹
        return $"\"{source}\"";
    }

    private List<VideoIdTagEntity> GetVideoIdTagEntities()
    {
        List<VideoIdTagEntity> re = new List<VideoIdTagEntity>();
        var filePath = "Data/videoIdTag.json";
        if (!File.Exists(filePath)) return re;

        try
        {
            var jsonStr = File.ReadAllText(filePath);

            return JsonConvert.DeserializeObject<List<VideoIdTagEntity>>(jsonStr);
        }
        catch (Exception e)
        {
            Logger.LogException(e);
            return re;
        }
    }

    private void InsertAndDeleteVideoIdTags(List<string> videoIds)
    {
        if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");
        if (videoIds == null) return;

        var existList = GetVideoIdTagEntities();

        var list = videoIds.Select(x => new VideoIdTagEntity(x)).ToList();
        list.AddRange(existList);

        if (list.Count > 9) list = list.Take(9).ToList();

        var jsonStr = JsonConvert.SerializeObject(list);
        Logger.LogInformation("当前tag：{tag}", jsonStr);

        File.WriteAllText("Data/videoIdTag.json", jsonStr);
    }
}
