using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AnimeThemes.Configuration;
using Jellyfin.Plugin.AnimeThemes.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Video = Jellyfin.Plugin.AnimeThemes.Models.Video;

namespace Jellyfin.Plugin.AnimeThemes;

/// <summary>
/// Class that is responsible for downloading themes.
/// </summary>
public class AnimeThemesDownloader : IDisposable
{
    private const string ThemeMusicFileName = "theme.mp3";
    private const string ThemeMusicDirectory = "theme-music";
    private const string ThemeVideoDirectory = "backdrops";

    private readonly HttpClient _client;
    private readonly AnimeThemesApi _api;
    private readonly ILogger<AnimeThemesDownloader> _logger;
    private readonly IMediaEncoder _mediaEncoder;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimeThemesDownloader"/> class.
    /// </summary>
    /// <param name="mediaEncoder">Media Encoder to convert OGG files.</param>
    /// <param name="logger">Logger.</param>
    public AnimeThemesDownloader(IMediaEncoder mediaEncoder, ILogger<AnimeThemesDownloader> logger)
    {
        _mediaEncoder = mediaEncoder;
        _logger = logger;
        _client = new HttpClient();
        _api = new AnimeThemesApi(logger);
    }

    /// <summary>
    /// Processes an item, downloading its theme if applicable.
    /// </summary>
    /// <param name="item">The DB item to process.</param>
    /// <param name="configuration">Configuration of the plugin.</param>
    /// <param name="redownload">Redownload all files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that runts until item is done processing.</returns>
    public async ValueTask HandleAsync(BaseItem item, PluginConfiguration configuration, bool redownload, CancellationToken cancellationToken)
    {
        // Get AniDB ID
        if (!TryGetAniDbId(item, configuration, out var id))
        {
            _logger.LogDebug("[{Id}] Item could not be identified as anime", item.Id);
            return;
        }

        if (IsSatisfied(item, configuration) && !redownload)
        {
            _logger.LogDebug("[{Id}] Item is already in a good state.", item.Id);
            return;
        }

        _logger.LogInformation("[{Id}] Getting theme songs for item", item.Id);

        // Get Anime
        var anime = await _api.FindByAniDbId(id, cancellationToken).ConfigureAwait(false);
        if (anime == null || !anime.Resources.Any(resource => resource.Site == Sites.ANIDB && resource.ExternalId == id))
        {
            return;
        }

        _logger.LogInformation("[{Id}] Attempting to filter theme songs for: {Name} (AniDB={AniId})", item.Id, item.Name, id);

        // Get audio
        var themes = GetBestThemes(anime, configuration).DistinctBy(it => it.Theme.Id).ToArray();

        _logger.LogInformation("[{Id}] Found {Count} entries", item.Id, themes.Length);

        // Process videos
        await ProcessMediaType(MediaType.Video, themes, item, configuration, redownload, cancellationToken).ConfigureAwait(false);

        // Process audios
        await ProcessMediaType(MediaType.Audio, themes, item, configuration, redownload, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask ProcessMediaType(MediaType type, FlattenedTheme[] themes, BaseItem item, PluginConfiguration configuration, bool redownload = false, CancellationToken cancellationToken = default)
    {
        bool isAudio = type == MediaType.Audio;

        if (redownload)
        {
            // Remove old files
            if (isAudio)
            {
                RemoveFile(item, ThemeMusicFileName);
                RemoveDirectory(item, ThemeMusicDirectory);
            }
            else
            {
                RemoveDirectory(item, ThemeVideoDirectory);
            }
        }

        var fetchType = isAudio ? configuration.AudioFetchType : configuration.VideoFetchType;
        var volume = isAudio ? configuration.AudioVolume : configuration.VideoVolume;

        if (fetchType == FetchType.Single)
        {
            // Pick best theme
            var bestTheme = themes.FirstOrDefault();
            if (bestTheme == null)
            {
                return;
            }

            _logger.LogInformation("[{Id}] Best theme = {Theme}", item.Id, bestTheme);

            // Download file
            var link = isAudio ? bestTheme.Audio.Link : bestTheme.Video.Link;
            var fileName = isAudio ? ThemeMusicFileName : Path.Combine(ThemeVideoDirectory, bestTheme.Theme.Slug + ".webm");
            await Download(type, link, item, fileName, volume, cancellationToken).ConfigureAwait(false);
        }
        else if (fetchType == FetchType.All)
        {
            // Download them all into "theme-music"
            foreach (var theme in themes)
            {
                var link = isAudio ? theme.Audio.Link : theme.Video.Link;
                var fileName = isAudio
                    ? Path.Combine(ThemeMusicDirectory, theme.Theme.Slug + ".mp3")
                    : Path.Combine(ThemeVideoDirectory, theme.Theme.Slug + ".webm");

                await Download(type, link, item, fileName, volume, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void RemoveFile(BaseItem series, string filename)
    {
        var path = Path.Combine(series.Path, "theme.mp3");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void RemoveDirectory(BaseItem series, string directory)
    {
        var path = Path.Combine(series.Path, directory);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    private async ValueTask Download(MediaType type, string url, BaseItem item, string filename, double volume = 1.0, CancellationToken cancellationToken = default)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var path = Path.Combine(item.Path, filename);
            // Make sure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            {
                _logger.LogInformation("[{Id}] Downloading {Url} to {Path}", item.Id, url, path);
                using var downloadStream = await _client.GetStreamAsync(url, cancellationToken).ConfigureAwait(false);
                using var fileStream = File.OpenWrite(tempFile);

                // Download file
                await downloadStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            // Convert OGG
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both or ffmpeg may hang due to deadlocks.
                    RedirectStandardOutput = true,
                    FileName = _mediaEncoder.EncoderPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                    ArgumentList = { "-i", tempFile }
                },
                EnableRaisingEvents = true
            };

            // Update arguments
            var arguments = process.StartInfo.ArgumentList;
            if (type == MediaType.Video)
            {
                // Copy video stream
                arguments.Add("-c:v");
                arguments.Add("copy");
            }

            if (volume < 0.01 && type == MediaType.Video)
            {
                // Mute videos when volume is low enough
                arguments.Add("-an");
            }
            else
            {
                arguments.Add("-filter:a");
                arguments.Add($"volume={volume:0.00}");
            }

            arguments.Add(path);

            process.Start();
            var memoryStream = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("[{Id}] Successfully downloaded theme song!", item.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Download failed");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private bool IsSatisfied(BaseItem item, PluginConfiguration configuration)
    {
        var audioSatisfied = item.GetThemeSongs().Any() || configuration.AudioFetchType == FetchType.None;
        var videoSatisfied = item.GetThemeVideos().Any() || configuration.VideoFetchType == FetchType.None;

        return audioSatisfied && videoSatisfied;
    }

    private bool TryGetAniDbId(BaseItem item, PluginConfiguration configuration, out int id)
    {
        id = -1;

        // Ignore non-series and already processed ones.
        if (item.GetBaseItemKind() != BaseItemKind.Series)
        {
            return false;
        }

        if (item.TryGetProviderId("AniDB", out var idAsString))
        {
            id = int.Parse(idAsString, CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets themes roughly sorted by relevance and filtered as needed.
    /// </summary>
    /// <param name="anime">The anime in question.</param>
    /// <param name="configuration">The configuration that sets the rules.</param>
    /// <returns>A filtered and sorted and flattened enumerable of themes.</returns>
    private IEnumerable<FlattenedTheme> GetBestThemes(Anime anime, PluginConfiguration configuration)
    {
        return anime.Themes.SelectMany(theme => theme.Entries.SelectMany(entry => entry.Videos.Select(video => Wrap(theme, entry, video))))
            .OrderBy(Rate)
            .Where(it => !configuration.IgnoreOverlapping || it.Video.Overlap == OverlapType.None)
            .Where(it => !configuration.IgnoreThemesWithCredits || it.Video.Creditless)
            .Where(it => !configuration.IgnoreEDs || it.Theme.Type != ThemeType.ED)
            .Where(it => !configuration.IgnoreOPs || it.Theme.Type != ThemeType.OP);
    }

    private FlattenedTheme Wrap(AnimeTheme theme, AnimeThemeEntry entry, Video video)
    {
        return new FlattenedTheme(theme, entry, video, video.Audio);
    }

    private double Rate(FlattenedTheme theme)
    {
        double score = 0;

        if (theme.Entry.Nsfw)
        {
            score += 10;
        }

        if (theme.Entry.Spoiler)
        {
            // Huge penalty for spoilers
            score += 50;
        }

        // Big penalties for overlap
        switch (theme.Video.Overlap)
        {
            case OverlapType.Over:
                score += 20;
                break;
            case OverlapType.Transition:
                score += 15;
                break;
        }

        // Small penalties for source
        switch (theme.Video.Source)
        {
            case VideoSource.LD:
            case VideoSource.VHS:
                score += 10;
                break;
            case VideoSource.WEB:
            case VideoSource.RAW:
                score += 5;
                break;
        }

        // Medium penalty for credits
        if (!theme.Video.Creditless)
        {
            score += 10;
        }

        return score;
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    /// <param name="disposing">Whether we're disposing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _api.Dispose();
            _client.Dispose();
        }
    }

    /// <summary>
    /// Dispose.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

internal record FlattenedTheme(AnimeTheme Theme, AnimeThemeEntry Entry, Video Video, Audio Audio);

internal enum MediaType
{
    Video,
    Audio
}
