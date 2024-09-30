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
        DegreeOfParallelism = 1;
        ForceSync = false;

        AudioSettings = new MediaTypeConfiguration()
        {
            FetchType = FetchType.Single,
            IgnoreOverlapping = true,
            IgnoreThemesWithCredits = false,
            IgnoreOPs = false,
            IgnoreEDs = false,
            Volume = 0.5,
        };

        VideoSettings = new MediaTypeConfiguration()
        {
            FetchType = FetchType.None,
            IgnoreOverlapping = true,
            IgnoreThemesWithCredits = true,
            IgnoreOPs = false,
            IgnoreEDs = false,
            Volume = 0.0,
        };
    }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public int DegreeOfParallelism { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the sync should enforce conformity.
    /// </summary>
    public bool ForceSync { get; set; }

    /// <summary>
    /// Gets or sets the audio settings.
    /// </summary>
    public MediaTypeConfiguration AudioSettings { get; set; }

    /// <summary>
    /// Gets or sets the video settings.
    /// </summary>
    public MediaTypeConfiguration VideoSettings { get; set; }
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

/// <summary>
/// Configuration for a specific media type.
/// </summary>
public class MediaTypeConfiguration
{
    /// <summary>
    /// Gets or sets how to fetch files.
    /// </summary>
    public FetchType FetchType { get; set; }

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
    /// Gets or sets the volume setting.
    /// </summary>
    public double Volume { get; set; }
}
