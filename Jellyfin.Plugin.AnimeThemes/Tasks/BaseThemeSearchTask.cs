using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AnimeThemes.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeThemes.Tasks;

/// <summary>
/// Base class for search tasks.
/// </summary>
public abstract class BaseThemeSearchTask
{
    private readonly ILogger _logger;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseThemeSearchTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">Use manager.</param>
    /// <param name="logger">Logger.</param>
    protected BaseThemeSearchTask(ILibraryManager libraryManager, IUserManager userManager, ILogger logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Processes all relevant items and calls a callback function.
    /// </summary>
    /// <param name="progress">Progress indicator.</param>
    /// <param name="handler">Handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that runs until the process is done.</returns>
    protected async Task FindAndProcessAsync(IProgress<double> progress, Func<BaseItem, PluginConfiguration, CancellationToken, ValueTask> handler, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting theme search");
        var configuration = Plugin.Instance!.Configuration;

        // @formatter:off
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Series },
            IsVirtualItem = false,
            Recursive = true,
        });
        // @formatter:on

        var semaphore = new SemaphoreSlim(1, 1);
        int counter = 0;
        int count = items.Count;
        // Process in parallel
        await Parallel.ForEachAsync(items, new ParallelOptions() { CancellationToken = cancellationToken, MaxDegreeOfParallelism = configuration.DegreeOfParallelism }, async (item, ct) =>
        {
            await handler(item, configuration, ct).ConfigureAwait(false);
            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                counter++;
                progress.Report(counter / (double)count * 100);
            }
            finally
            {
                semaphore.Release();
            }
        }).ConfigureAwait(false);

        _logger.LogInformation("Ending theme search ({Count})", count);

        _libraryManager.QueueLibraryScan();
    }
}
