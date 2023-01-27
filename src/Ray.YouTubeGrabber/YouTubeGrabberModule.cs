using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ray.YouTubeGrabber.Apis;
using Refit;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Ray.YouTubeGrabber;

[DependsOn(
    typeof(AbpAutofacModule)
)]
public class YouTubeGrabberModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);

        var config = context.Services.GetConfiguration();

        context.Services.Configure<GrabberOptions>(x =>
        {
            x.BrowseId = config["GrabberConfig:BrowseId"];
            x.ParamsCode = config["GrabberConfig:Params"];
        });
        context.Services.Configure<HttpClientCustomOptions>(config.GetSection("HttpCustomConfig"));

        context.Services.AddScoped<ProxyHttpClientHandler>();
        context.Services.AddScoped<LogHttpMessageHandler>();

        var newtonSetting = new JsonSerializerSettings();
        //newtonSetting.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
        //newtonSetting.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
        var settings = new RefitSettings(new NewtonsoftJsonContentSerializer(newtonSetting));
        // Configure refit settings here

        context.Services.AddRefitClient<IYouTubeApi>(settings)
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://www.youtube.com");
                var ua = config["UserAgent"];
                if (!string.IsNullOrWhiteSpace(ua))
                    c.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
            })
            .AddHttpMessageHandler<LogHttpMessageHandler>()
            .ConfigurePrimaryHttpMessageHandler<ProxyHttpClientHandler>();
        // .SetHandlerLifetime(TimeSpan.FromMinutes(2));
    }

    public override Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<YouTubeGrabberModule>>();
        var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
        logger.LogInformation($"MySettingName => {configuration["MySettingName"]}");

        var hostEnvironment = context.ServiceProvider.GetRequiredService<IHostEnvironment>();
        logger.LogInformation($"EnvironmentName => {hostEnvironment.EnvironmentName}");

        return Task.CompletedTask;
    }
}
