using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.AnimeThemes.Models;

/// <summary>
/// Represents a response to the anime endpoint.
/// </summary>
/// <param name="Anime">List of found anime.</param>
public record AnimeResponse(
    [property: JsonPropertyName("anime")] Collection<Anime> Anime
);
