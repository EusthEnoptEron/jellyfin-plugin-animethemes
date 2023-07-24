using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.AnimeThemes.Models;

/// <summary>
/// A video API resource represents a WebM of an anime theme.
///
/// For example, the video Bakemonogatari-OP1.webm represents the WebM of the Bakemonogatari OP1 theme.
/// </summary>
/// <param name="Id">The primary key of the resource.</param>
/// <param name="Basename">The basename of the file in storage.</param>
/// <param name="Filename">The filename of the file in storage.</param>
/// <param name="Source">Where did this video come from? [WEB, RAW, BD, DVD, VHS, LD].</param>
/// <param name="Overlap">The degree to which the sequence and episode content overlap [None, Transition, Over].</param>
/// <param name="Link">The URL to stream the file from storage.</param>
/// <param name="Audio">Audio for the video.</param>
public record Video(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("basename")]
    string Basename,
    [property: JsonPropertyName("filename")]
    string Filename,
    [property: JsonPropertyName("source")] VideoSource? Source,
    [property: JsonPropertyName("overlap")]
    OverlapType Overlap,
    [property: JsonPropertyName("link")] string Link,
    [property: JsonPropertyName("audio")] Audio Audio
);
