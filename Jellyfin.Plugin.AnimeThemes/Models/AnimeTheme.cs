using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.AnimeThemes.Models;

/// <summary>
/// Represents an <a href="https://api-docs.animethemes.moe/animetheme/">anime theme</a>.
/// </summary>
/// <param name="Id">The primary key of the resource.</param>
/// <param name="Type">The type of the sequence [OP, ED].</param>
/// <param name="Sequence">The numeric ordering of the theme.</param>
/// <param name="Group">Used to distinguish sequence belongs to dubs, rebroadcasts, etc.</param>
/// <param name="Entries">Entries of the theme.</param>
public record AnimeTheme(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("type")] ThemeType? Type,
    [property: JsonPropertyName("sequence")]
    int? Sequence,
    [property: JsonPropertyName("group")] string? Group,
    [property: JsonPropertyName("animethemeentries")]
    Collection<AnimeThemeEntry> Entries
);
