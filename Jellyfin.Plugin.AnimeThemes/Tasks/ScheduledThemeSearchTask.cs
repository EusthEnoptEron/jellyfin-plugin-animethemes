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
public class ScheduledThemeSearchTask : BaseThemeSearchTask, IScheduledTask
{
    private readonly AnimeThemesDownloader _downloader;
    private readonly ILogger<ScheduledThemeSearchTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledThemeSearchTask"/> class.
    /// </summary>
    /// <param name="downloader">Downloader.</param>
    /// <param name="libraryManager">Library Manager.</param>
    /// <param name="userManager">User Manager.</param>
    /// <param name="logger">Logger.</param>
    public ScheduledThemeSearchTask(AnimeThemesDownloader downloader, ILibraryManager libraryManager, IUserManager userManager, ILogger<ScheduledThemeSearchTask> logger) : base(libraryManager, userManager, logger)
    {
        _logger = logger;
        _downloader = downloader;
    }

    /// <inheritdoc />
    public string Name { get; } = "Search For Anime Themes";

    /// <inheritdoc />
    public string Key { get; } = "animethemes.search";

    /// <inheritdoc />
    public string Description { get; } = "Updates all themes according to the settings. This also happens after every full scan.";

    /// <inheritdoc />
    public string Category { get; } = "Theme Songs";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting search for anime themes");
        return FindAndProcessAsync(progress, (item, config, ct) => _downloader.HandleAsync(item, config, ct), cancellationToken);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(6).Ticks };
    }
}
