// See https://aka.ms/new-console-template for more information


using Jellyfin.Plugin.AnimeThemes;
using Microsoft.Extensions.Logging;

var api = new AnimeThemesApi(new Logger<AnimeThemesDownloader>(new LoggerFactory()));
var result = await api.FindByAniDbId(8620);

Console.WriteLine(result);
