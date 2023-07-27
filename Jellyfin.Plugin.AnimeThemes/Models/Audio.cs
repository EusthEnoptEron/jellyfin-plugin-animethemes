using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.AnimeThemes.Models;

/// <summary>
/// An audio API resource represents the audio track of a video.
///
/// For example, the audio Bakemonogatari-OP1.ogg represents the audio track of the Bakemonogatari-OP1.webm video.
/// </summary>
/// <param name="Id">The primary key of the resource.</param>
/// <param name="Path">The path of the file in storage.</param>
/// <param name="Filename">The filename of the file without extension.</param>
/// <param name="MimeType">The media type of the file in storage.</param>
/// <param name="Link">The URL to stream the file from storage.</param>
public record Audio(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("mimetype")]
    string MimeType,
    [property: JsonPropertyName("link")] string Link
);
