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
        FetchAll = false;
        VolumeFactor = 0.5;
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
    /// Gets or sets a value indicating whether to fetch all themes.
    /// </summary>
    public bool FetchAll { get; set; }

    /// <summary>
    /// Gets or sets an volume setting.
    /// </summary>
    public double VolumeFactor { get; set; }

    /// <summary>
    /// Gets or sets a string setting.
    /// </summary>
    public int DegreeOfParallelism { get; set; }
}
