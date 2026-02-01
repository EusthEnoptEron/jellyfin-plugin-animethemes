using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AnimeThemes.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeThemes;

/// <summary>
/// Represents the AnimeThemes API.
/// </summary>
public sealed class AnimeThemesApi : IDisposable
{
    private readonly HttpClient _client;
    private readonly ILogger<AnimeThemesDownloader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimeThemesApi"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="clientFactory">Client factory.</param>
    public AnimeThemesApi(IHttpClientFactory clientFactory, ILogger<AnimeThemesDownloader> logger)
    {
        _logger = logger;
        _client = clientFactory.CreateClient("AnimeThemes");
        _client.DefaultRequestHeaders.UserAgent.Clear();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Jellyfin-Plugin-AnimeThemes/1.0");
        _logger.LogInformation("AnimeThemes Plugin: Custom User-Agent has been set.");
    }

    /// <summary>
    /// Finds an anime with all its themes by its AniDB Id.
    /// </summary>
    /// <param name="id">ID on AniDB.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The anime if one was found.</returns>
    public async ValueTask<Anime?> FindByAniDbId(int id, CancellationToken cancellationToken = default)
    {
        // @formatter:off
        var arguments = new Dictionary<string, string?>()
        {
            { "filter[resource][external_id]", id.ToString(CultureInfo.InvariantCulture) },
            { "filter[resource][site]", Sites.ANIDB },
            { "filter[has]", "resources" },
            { "include", "animethemes.animethemeentries.videos.audio,resources" },
        };
        // @formatter:on

        var uri = QueryHelpers.AddQueryString("/anime/", arguments);

        _logger.LogInformation("Fetching from API: {Uri}", uri);
        var result = await _client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        result.EnsureSuccessStatusCode();

        var content = await result.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var contentDisposal = content.ConfigureAwait(false);

        var response = await JsonSerializer.DeserializeAsync<AnimeResponse>(content, cancellationToken: cancellationToken).ConfigureAwait(false);
        return response?.Anime.FirstOrDefault();
    }

    /// <summary>
    /// Disposes of the client.
    /// </summary>
    /// <param name="disposing">Whether the object is currently being disposed.</param>
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
    }
}
