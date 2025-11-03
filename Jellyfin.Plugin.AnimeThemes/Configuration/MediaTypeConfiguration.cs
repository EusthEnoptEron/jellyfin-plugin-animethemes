namespace Jellyfin.Plugin.AnimeThemes.Configuration;

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
