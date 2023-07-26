using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeThemes.Tasks;

/// <summary>
/// Task that updates theme songs when needed.
/// </summary>
public sealed class ThemeSearchTask : BaseThemeSearchTask, ILibraryPostScanTask
{
    private readonly AnimeThemesDownloader _downloader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeSearchTask"/> class.
    /// </summary>
    /// <param name="downloader">Downloader.</param>
    /// <param name="manager">Manager.</param>
    /// <param name="logger">Logger.</param>
    public ThemeSearchTask(AnimeThemesDownloader downloader, ILibraryManager manager, ILogger<ThemeSearchTask> logger) : base(manager, logger)
    {
        _downloader = downloader;
    }

    /// <inheritdoc />
    public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        return FindAndProcessAsync(progress, (item, config, ct) => _downloader.HandleAsync(item, config, false, ct), cancellationToken);
    }
}
