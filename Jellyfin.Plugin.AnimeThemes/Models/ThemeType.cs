using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.AnimeThemes.Models;

/// <summary>
/// Enum describing the theme types.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ThemeType
{
    /// <summary>
    /// Opening theme.
    /// </summary>
    OP,

    /// <summary>
    /// Ending theme.
    /// </summary>
    ED
}
