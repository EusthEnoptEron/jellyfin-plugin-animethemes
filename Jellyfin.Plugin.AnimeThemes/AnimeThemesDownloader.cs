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
using Microsoft.Extensions.Logging;
using Video = Jellyfin.Plugin.AnimeThemes.Models.Video;

namespace Jellyfin.Plugin.AnimeThemes;

/// <summary>
/// Class that is responsible for downloading themes.
/// </summary>
public class AnimeThemesDownloader : IDisposable
{
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that runts until item is done processing.</returns>
    public async ValueTask Process(BaseItem item, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Id}] Getting theme song for item", item.Id);

        // Get AniDB ID
        if (!TryGetAniDbId(item, out var id))
        {
            return;
        }

        // Get Anime
        var anime = await _api.FindByAniDbId(id, cancellationToken).ConfigureAwait(false);
        if (anime == null || !anime.Resources.Any(resource => resource.Site == Sites.ANIDB && resource.ExternalId == id))
        {
            return;
        }

        _logger.LogInformation("[{Id}] Attempting to get theme song for anime: {Name} (AniDB={AniId})", item.Id, item.Name, id);

        // Get audio
        var audios = GetBestAudios(anime, configuration).ToArray();

        _logger.LogInformation("[{Id}] Found {Count} entries", item.Id, audios.Length);

        if (configuration.FetchAll)
        {
            // Download them all into "theme-music"
            var themes = audios.DistinctBy(it => it.Theme.Id);
            foreach (var theme in themes)
            {
                await Download(theme.Audio, item, Path.Combine("theme-music", theme.Theme.Slug + ".mp3"), configuration.VolumeFactor, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            // Pick best audio
            var bestAudio = audios.Select(it => it.Audio).FirstOrDefault();
            if (bestAudio == null)
            {
                return;
            }

            _logger.LogInformation("[{Id}] Best audio = {Audio}", item.Id, bestAudio);

            // Download audio
            await Download(bestAudio, item, volumeFactor: configuration.VolumeFactor, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask Download(Audio audio, BaseItem item, string filename = "theme.mp3", double volumeFactor = 1.0, CancellationToken cancellationToken = default)
    {
        try
        {
            var tempFile = Path.GetTempFileName();
            var path = Path.Combine(item.Path, filename);
            // Make sure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            {
                _logger.LogInformation("[{Id}] Downloading {Url} to {Path}", item.Id, audio.Link, path);
                using var downloadStream = await _client.GetStreamAsync(audio.Link, cancellationToken).ConfigureAwait(false);
                using var fileStream = File.OpenWrite(tempFile);

                // Download OGG
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
                    ArgumentList =
                    {
                        "-i",
                        tempFile,
                        "-filter:a",
                        $"volume={volumeFactor:0.00}",
                        path
                    }
                },
                EnableRaisingEvents = true
            };

            process.Start();
            var memoryStream = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

            File.Delete(tempFile);

            _logger.LogInformation("[{Id}] Successfully downloaded theme song!", item.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Download failed");
        }
    }

    private bool TryGetAniDbId(BaseItem item, out int id)
    {
        id = -1;

        // Ignore non-series and already processed ones.
        if (item.GetBaseItemKind() != BaseItemKind.Series || item.GetThemeSongs().Count > 0)
        {
            _logger.LogInformation("[{Id}] Item was disqualified", item.Id);
            return false;
        }

        if (item.TryGetProviderId("AniDB", out var idAsString))
        {
            id = int.Parse(idAsString, CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }

    private IEnumerable<(AnimeTheme Theme, AnimeThemeEntry Entry, Video Video, Audio Audio)> GetBestAudios(Anime anime, PluginConfiguration configuration)
    {
        return anime.Themes.SelectMany(theme => theme.Entries.SelectMany(entry => entry.Videos.Select(video => Wrap(theme, entry, video))))
            .OrderBy(Rate)
            .Where(it => !configuration.IgnoreOverlapping || it.Video.Overlap == OverlapType.None)
            .Where(it => !configuration.IgnoreEDs || it.Theme.Type != ThemeType.ED)
            .Where(it => !configuration.IgnoreOPs || it.Theme.Type != ThemeType.OP);
    }

    private (AnimeTheme Theme, AnimeThemeEntry Entry, Video Video, Audio Audio) Wrap(AnimeTheme theme, AnimeThemeEntry entry, Video video)
    {
        return (theme, entry, video, video.Audio);
    }

    private double Rate((AnimeTheme Theme, AnimeThemeEntry Entry, Video Video, Audio Audio) tuple)
    {
        double score = 0;

        if (tuple.Entry.Nsfw)
        {
            score += 10;
        }

        if (tuple.Entry.Spoiler)
        {
            score += 10;
        }

        switch (tuple.Video.Overlap)
        {
            case OverlapType.Over:
                score += 10;
                break;
            case OverlapType.Transition:
                score += 5;
                break;
        }

        switch (tuple.Video.Source)
        {
            case VideoSource.LD:
            case VideoSource.VHS:
                score += 5;
                break;
            case VideoSource.WEB:
            case VideoSource.RAW:
                score += 2;
                break;
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
