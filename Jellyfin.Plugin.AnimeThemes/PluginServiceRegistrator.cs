using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.AnimeThemes;

/// <inheritdoc />
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<AnimeThemesDownloader>();
    }
}
