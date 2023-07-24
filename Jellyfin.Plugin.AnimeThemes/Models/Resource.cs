using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.AnimeThemes.Models;

/// <summary>
/// Represents a resource. Subset of <a href="https://api-docs.animethemes.moe/resource">resource</a>.
///
/// For site types, see <see cref="Sites"/>.
/// </summary>
public record Resource(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("external_id")]
    int ExternalId,
    [property: JsonPropertyName("site")] string Site
);
