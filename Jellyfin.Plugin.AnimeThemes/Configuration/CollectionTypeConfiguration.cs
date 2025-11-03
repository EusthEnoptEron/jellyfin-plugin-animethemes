namespace Jellyfin.Plugin.AnimeThemes.Configuration;

/// <summary>
/// Configuration for a collection type.
/// </summary>
public class CollectionTypeConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionTypeConfiguration"/> class.
    /// </summary>
    public CollectionTypeConfiguration()
    {
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
    /// Gets or sets the audio settings.
    /// </summary>
    public MediaTypeConfiguration AudioSettings { get; set; }

    /// <summary>
    /// Gets or sets the video settings.
    /// </summary>
    public MediaTypeConfiguration VideoSettings { get; set; }
}
