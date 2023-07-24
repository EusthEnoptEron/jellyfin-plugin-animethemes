using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeThemes;

/// <summary>
/// Task that updates theme songs when needed.
/// </summary>
public sealed class ThemeSearchTask : ILibraryPostScanTask, IDisposable
{
    private readonly ILibraryManager _manager;
    private readonly AnimeThemesDownloader _downloader;
    private readonly ILogger<AnimeThemesDownloader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeSearchTask"/> class.
    /// </summary>
    /// <param name="manager">Manager.</param>
    /// <param name="encoder">Encoder.</param>
    /// <param name="logger">Logger.</param>
    public ThemeSearchTask(ILibraryManager manager, IMediaEncoder encoder, ILogger<AnimeThemesDownloader> logger)
    {
        _logger = logger;
        _downloader = new AnimeThemesDownloader(encoder, logger);
        _manager = manager;
    }

    /// <inheritdoc />
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting theme search");

        // @formatter:off
        var items = _manager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Series },
            IsVirtualItem = false,
            Recursive = true,
            HasThemeSong = false,
        });
        // @formatter:on

        int count = 0;
        foreach (var item in items)
        {
            await _downloader.Process(item, cancellationToken).ConfigureAwait(false);
            if (count++ > 5)
            {
                break;
            }
        }

        _logger.LogInformation("Ending theme search ({Count})", items.Count);
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        _downloader.Dispose();
    }
}
