using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AnimeThemes.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // set default options here
        IgnoreOverlapping = true;
        IgnoreEDs = false;
        IgnoreOPs = false;
        IgnoreThemesWithCredits = false;
        AudioVolume = 0.5;
        VideoVolume = 0.0;
        VideoFetchType = FetchType.None;
        AudioFetchType = FetchType.Single;
        DegreeOfParallelism = 1;
    }

    /// <summary>
    /// Gets or sets a value indicating whether some true or false setting is enabled..
    /// </summary>
    public bool IgnoreOverlapping { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore ED themes.
    /// </summary>
    public bool IgnoreEDs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore OP themes.
    /// </summary>
    public bool IgnoreOPs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore themes with credits in them.
    /// </summary>
    public bool IgnoreThemesWithCredits { get; set; }

    /// <summary>
    /// Gets or sets an volume setting.
    /// </summary>
    public double AudioVolume { get; set; }

    /// <summary>
    /// Gets or sets an volume setting.
    /// </summary>
    public double VideoVolume { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public int DegreeOfParallelism { get; set; }

    /// <summary>
    /// Gets or sets how to fetch videos.
    /// </summary>
    public FetchType VideoFetchType { get; set; }

    /// <summary>
    /// Gets or sets how to fetch audio files.
    /// </summary>
    public FetchType AudioFetchType { get; set; }
}

/// <summary>
/// Enum describing how files should be fetched.
/// </summary>
public enum FetchType
{
    /// <summary>
    /// Download nothing.
    /// </summary>
    None,

    /// <summary>
    /// Download only one file.
    /// </summary>
    Single,

    /// <summary>
    /// Download all files.
    /// </summary>
    All
}
