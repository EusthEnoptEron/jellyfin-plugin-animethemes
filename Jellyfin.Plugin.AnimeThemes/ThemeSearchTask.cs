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
        var configuration = Plugin.Instance!.Configuration;

        // @formatter:off
        var items = _manager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Series },
            IsVirtualItem = false,
            Recursive = true,
            HasThemeSong = false,
        });
        // @formatter:on

        var semaphore = new SemaphoreSlim(1, 1);
        int counter = 0;
        int count = items.Count;
        // Process in parallel
        await Parallel.ForEachAsync(items, new ParallelOptions() { CancellationToken = cancellationToken, MaxDegreeOfParallelism = configuration.DegreeOfParallelism }, async (item, ct) =>
        {
            await _downloader.Process(item, configuration, ct).ConfigureAwait(false);
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                counter++;
                progress.Report(counter / (double)count);
            }
            finally
            {
                semaphore.Release();
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Ending theme search ({Count})", count);
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        _downloader.Dispose();
    }
}
