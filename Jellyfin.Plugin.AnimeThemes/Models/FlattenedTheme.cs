namespace Jellyfin.Plugin.AnimeThemes.Models;

internal sealed record FlattenedTheme(AnimeTheme Theme, AnimeThemeEntry Entry, Video Video, Audio Audio);
