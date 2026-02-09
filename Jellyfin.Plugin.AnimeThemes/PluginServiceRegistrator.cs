using System;
using System.Net.Http.Headers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AnimeThemes;

/// <inheritdoc />
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddScoped<AnimeThemesDownloader>();

        var productHeader = new ProductInfoHeaderValue(
            "jf-plugin-animethemes",
            applicationHost.ApplicationVersionString);

        serviceCollection.AddTransient<PollyResilienceHandler>();

        serviceCollection
            .AddHttpClient("AnimeThemes", c =>
            {
                c.BaseAddress = new Uri("https://api.animethemes.moe");
                c.DefaultRequestHeaders.UserAgent.Add(productHeader);
            })
            .AddHttpMessageHandler<PollyResilienceHandler>();
    }
}
