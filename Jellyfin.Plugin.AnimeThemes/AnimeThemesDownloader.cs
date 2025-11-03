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
using Jellyfin.Plugin.AnimeThemes.Exceptions;
using Jellyfin.Plugin.AnimeThemes.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaType = Jellyfin.Plugin.AnimeThemes.Models.MediaType;
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that runts until item is done processing.</returns>
    public async ValueTask HandleAsync(BaseItem item, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        // Get AniDB ID
        if (!TryGetAniDbId(item, configuration, out var id))
        {
            _logger.LogDebug("[{Id}] Item could not be identified as anime", item.Id);
            return;
        }

        if (IsSatisfied(item, configuration) && !configuration.ForceSync)
        {
            _logger.LogDebug("[{Id}] Item is already in a good state", item.Id);
            return;
        }

        _logger.LogInformation("[{Id}] Getting theme songs for item \"{Name}\"", item.Id, item.Name);

        // Get Anime with all its themes
        var anime = await _api.FindByAniDbId(id, cancellationToken).ConfigureAwait(false);
        if (anime == null || !anime.Resources.Any(resource => resource.Site == Sites.ANIDB && resource.ExternalId == id))
        {
            return;
        }

        _logger.LogInformation("[{Id}] Attempting to filter theme songs for: {Name} (AniDB={AniId})", item.Id, item.Name, id);

        bool isMovie = item.GetBaseItemKind() == BaseItemKind.Movie;
        var collectionTypeConfig = isMovie ? configuration.MovieSettings : new CollectionTypeConfiguration { AudioSettings = configuration.AudioSettings, VideoSettings = configuration.VideoSettings };

        // Process videos
        bool videoChanged = await ProcessMediaType(MediaType.Video, anime, item, configuration.ForceSync, collectionTypeConfig, cancellationToken).ConfigureAwait(false);

        // Process audios
        bool audioChanged = await ProcessMediaType(MediaType.Audio, anime, item, configuration.ForceSync, collectionTypeConfig, cancellationToken).ConfigureAwait(false);

        if (videoChanged || audioChanged)
        {
            _logger.LogInformation("[{Id}] Saving metadata", item.Id);
            await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<bool> ProcessMediaType(MediaType type, Anime anime, BaseItem item, bool forceSync, CollectionTypeConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var settings = type == MediaType.Audio ? configuration.AudioSettings : configuration.VideoSettings;

        var distinctThemes = GetBestThemes(anime, settings).DistinctBy(it => it.Theme.Id);

        // Pick themes according to fetch type
        var requiredThemes = PickThemes(settings.FetchType, distinctThemes);

        // Turn them into downloadable links
        var links = ExtractLinks(type, requiredThemes, settings).ToArray();

        // Before we start the download, make sure the folders are in a clean state.
        if (forceSync)
        {
            if (type == MediaType.Audio)
            {
                // We don't need this file because we're using the directory variant.
                RemoveFile(item, ThemeMusicFileName);
            }

            CleanDirectory(item, type, links.Select(it => Path.GetFileName(it.Filepath)));
        }

        bool changesMade = false;
        foreach (var (url, relativePath) in links)
        {
            // Download if needed
            changesMade |= await Download(type, url, item, relativePath, settings.Volume, cancellationToken).ConfigureAwait(false);
        }

        return changesMade;
    }

    private IEnumerable<FlattenedTheme> PickThemes(FetchType fetchType, IEnumerable<FlattenedTheme> themes)
    {
        switch (fetchType)
        {
            case FetchType.None:
                return Enumerable.Empty<FlattenedTheme>();
            case FetchType.Single:
                return themes.Take(1);
            case FetchType.All:
                return themes;
            default:
                throw new ArgumentOutOfRangeException($"Unknown fetch type: {fetchType}");
        }
    }

    private IEnumerable<(string Url, string Filepath)> ExtractLinks(MediaType type, IEnumerable<FlattenedTheme> themes, MediaTypeConfiguration settings)
    {
        bool isAudio = type == MediaType.Audio;

        foreach (var theme in themes)
        {
            var link = isAudio ? theme.Audio.Link : theme.Video.Link;
            var path = isAudio
                ? Path.Combine(ThemeMusicDirectory, $"{theme.Audio.Filename}__{settings.Volume * 100:0}.mp3")
                : Path.Combine(ThemeVideoDirectory, $"{theme.Video.Filename}__{settings.Volume * 100:0}.webm");

            yield return (link, path);
        }
    }

    private void RemoveFile(BaseItem series, string filename)
    {
        var path = Path.Combine(series.ContainingFolderPath, filename);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void CleanDirectory(BaseItem series, MediaType mediaType, IEnumerable<string> allowedNames)
    {
        var directory = mediaType == MediaType.Audio ? ThemeMusicDirectory : ThemeVideoDirectory;
        var searchPattern = mediaType == MediaType.Audio ? "*.mp3" : "*.webm";

        var path = Path.Combine(series.ContainingFolderPath, directory);
        if (!Directory.Exists(path))
        {
            return;
        }

        var allowedNamesSet = allowedNames.ToHashSet();

        foreach (var filepath in Directory.GetFiles(path, searchPattern))
        {
            var name = Path.GetFileName(filepath);
            if (!allowedNamesSet.Contains(name))
            {
                _logger.LogInformation("[{Id}] Removing obsolete theme: {Theme}", series.Id, filepath);
                File.Delete(filepath);
            }
        }
    }

    private async ValueTask<bool> Download(MediaType type, string url, BaseItem item, string relativePath, double volume = 1.0, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(item.ContainingFolderPath, relativePath);
        if (File.Exists(path))
        {
            // Nothing to do
            return false;
        }

        var tempFile = Path.GetTempFileName();
        try
        {
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
                    RedirectStandardError = true,
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
                arguments.Add(string.Create(CultureInfo.InvariantCulture, $"volume={volume:0.00}"));
            }

            arguments.Add(path);

            process.Start();

            var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var commandInfo = $"Command line: {process.StartInfo.FileName} {string.Join(" ", arguments)}";
                throw new ConversionException(process.ExitCode, commandInfo + "\n" + error);
            }

            _logger.LogInformation("[{Id}] Successfully downloaded theme song!", item.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Download failed");
            return false;
        }
        finally
        {
            File.Delete(tempFile);
        }

        return true;
    }

    private bool IsSatisfied(BaseItem item, PluginConfiguration configuration)
    {
        var audioSatisfied = item.GetThemeSongs().Any() || configuration.AudioSettings.FetchType == FetchType.None;
        var videoSatisfied = item.GetThemeVideos().Any() || configuration.VideoSettings.FetchType == FetchType.None;

        return audioSatisfied && videoSatisfied;
    }

    private bool TryGetAniDbId(BaseItem item, PluginConfiguration configuration, out int id)
    {
        id = -1;

        // Ignore non-series and already processed ones.
        if (item.GetBaseItemKind() != BaseItemKind.Series && item.GetBaseItemKind() != BaseItemKind.Movie)
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
    /// <param name="settings">The configuration that sets the rules.</param>
    /// <returns>A filtered and sorted and flattened enumerable of themes.</returns>
    private IEnumerable<FlattenedTheme> GetBestThemes(Anime anime, MediaTypeConfiguration settings)
    {
        return anime.Themes.SelectMany(theme => theme.Entries.SelectMany(entry => entry.Videos.Select(video => Wrap(theme, entry, video))))
            .OrderBy(Rate)
            .Where(it => !settings.IgnoreOverlapping || it.Video.Overlap == OverlapType.None)
            .Where(it => !settings.IgnoreThemesWithCredits || it.Video.Creditless)
            .Where(it => !settings.IgnoreEDs || it.Theme.Type != ThemeType.ED)
            .Where(it => !settings.IgnoreOPs || it.Theme.Type != ThemeType.OP);
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
