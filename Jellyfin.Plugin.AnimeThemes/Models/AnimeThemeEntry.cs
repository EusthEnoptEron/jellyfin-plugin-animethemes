using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.AnimeThemes.Models;

/// <summary>
/// An anime theme entry API resource represents a version of an anime theme.
///
/// For example, the ED theme of the Bakemonogatari anime has three anime theme entries to represent three versions.
/// </summary>
/// <param name="Id">The primary key of the resource.</param>
/// <param name="Version">The version number of the theme.</param>
/// <param name="Episodes">The episodes that the theme is used for.</param>
/// <param name="Nsfw">Whether not safe for work content is included.</param>
/// <param name="Spoiler">Whether content is included that may spoil the viewer.</param>
/// <param name="Videos">Videos for the entry.</param>
public record AnimeThemeEntry(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("version")]
    int? Version,
    [property: JsonPropertyName("episodes")]
    string? Episodes,
    [property: JsonPropertyName("nsfw")] bool Nsfw,
    [property: JsonPropertyName("spoiler")]
    bool Spoiler,
    [property: JsonPropertyName("videos")] Collection<Video> Videos
);
