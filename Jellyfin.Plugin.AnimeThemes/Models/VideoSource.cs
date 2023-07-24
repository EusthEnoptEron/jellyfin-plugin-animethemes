using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.AnimeThemes.Models;

/// <summary>
/// Enum of all possible video sources.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VideoSource
{
    /// <summary>
    /// WEB.
    /// </summary>
    WEB,

    /// <summary>
    /// RAW.
    /// </summary>
    RAW,

    /// <summary>
    /// Bluray Disc.
    /// </summary>
    BD,

    /// <summary>
    /// DVD.
    /// </summary>
    DVD,

    /// <summary>
    /// VHS.
    /// </summary>
    VHS,

    /// <summary>
    /// LD.
    /// </summary>
    LD
}
