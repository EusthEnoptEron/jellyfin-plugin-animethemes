using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeThemes.Tasks;

/// <summary>
/// Scheduled task for enforcing configuration.
/// </summary>
public class EnforcedThemeSearchTask : BaseThemeSearchTask, IScheduledTask
{
    private readonly AnimeThemesDownloader _downloader;
    private readonly ILogger<EnforcedThemeSearchTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnforcedThemeSearchTask"/> class.
    /// </summary>
    /// <param name="downloader">Downloader.</param>
    /// <param name="manager">Manager.</param>
    /// <param name="logger">Logger.</param>
    public EnforcedThemeSearchTask(AnimeThemesDownloader downloader, ILibraryManager manager, ILogger<EnforcedThemeSearchTask> logger) : base(manager, logger)
    {
        _logger = logger;
        _downloader = downloader;
    }

    /// <inheritdoc />
    public string Name { get; } = "Redownload All Anime Themes";

    /// <inheritdoc />
    public string Key { get; } = "animethemes.fullupdate";

    /// <inheritdoc />
    public string Description { get; } = "Updates all themes according to the settings. This will enforce these settings and delete files that shouldn't be there. It's not recommended to schedule this task. The normal scan will run anyway at the end of every library scan.";

    /// <inheritdoc />
    public string Category { get; } = "Theme Songs";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting full update, enforcing all rules");
        return FindAndProcessAsync(progress, (item, config, ct) => _downloader.HandleAsync(item, config, true, ct), cancellationToken);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // No triggers
        yield break;
    }
}
