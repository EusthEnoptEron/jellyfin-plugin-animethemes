using System.Collections.ObjectModel;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AnimeThemes.Models;

/// <summary>
/// BaseItem and the corresponding anime object.
/// </summary>
/// <param name="Item">BaseItem object.</param>
/// <param name="Anime">Anime object.</param>
public sealed record ItemWithAnime(BaseItem Item, ReadOnlyCollection<Anime> Anime);
