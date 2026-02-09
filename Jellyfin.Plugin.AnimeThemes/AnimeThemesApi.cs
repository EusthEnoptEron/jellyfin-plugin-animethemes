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
    }

    /// <summary>
    /// Finds anime with all their themes by their AniDB Ids.
    /// </summary>
    /// <param name="ids">IDs on AniDB.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary with an entry for each passed <paramref name="ids"/>.</returns>
    public async ValueTask<Dictionary<int, Anime[]>> FindByAniDbId(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToArray();

        _logger.LogInformation("Looking up {Count} shows in the AnimeThemes db...", idList.Length);

        if (idList.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(ids));
        }

        // @formatter:off
        var arguments = new Dictionary<string, string?>
        {
            { "filter[resource][external_id]", string.Join(",", idList.Select(it => it.ToString(CultureInfo.InvariantCulture))) },
            { "filter[resource][site]", Sites.ANIDB },
            { "filter[has]", "resources" },
            { "page[size]", idList.Length.ToString(CultureInfo.InvariantCulture) },
            { "include", "animethemes.animethemeentries.videos.audio,resources" },
        };
        // @formatter:on

        var uri = QueryHelpers.AddQueryString("/anime/", arguments);

        _logger.LogInformation("Fetching from API: {Uri} (length={Length})", uri, uri.Length);
        var result = await _client.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        result.EnsureSuccessStatusCode();

        var content = await result.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var contentDisposal = content.ConfigureAwait(false);

        var response = await JsonSerializer.DeserializeAsync<AnimeResponse>(content, cancellationToken: cancellationToken).ConfigureAwait(false);
        var animeByAniId = response?.Anime.GroupBy(a => a.AniDbId!.Value).ToDictionary(a => a.Key, a => a.ToArray()) ?? new Dictionary<int, Anime[]>();

        _logger.LogInformation("Of a total of {TotalCount} shows, we were able to find entries for {FinalCount} shows", idList.Length, animeByAniId.Count);

        return idList.ToDictionary(id => id, id => animeByAniId.GetValueOrDefault(id) ?? []);
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
