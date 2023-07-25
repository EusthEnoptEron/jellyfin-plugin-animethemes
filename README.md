# AnimeThemes for Jellyfin

Experimental Plugin for [Jellyfin](https://jellyfin.org/) that fetches anime themes (OPs and EDs)
from [animethemes.moe](https://animethemes.moe).

**Since the AniDB ID is used to find the matching themes, you'll need to install the AniDB plugin.**

## How to use

Currently the scan runs as a `ILibraryPostScanTask`, meaning it will run after a full scan of your library. So:

1. Run a full scan of your library.
2. Refresh metadata to find the new themes.

Also make sure to enable theme music / videos in the display settings.

## Configuration

| Configuration                           | Default | Description                                                                                                                 |
|-----------------------------------------|---------|-----------------------------------------------------------------------------------------------------------------------------|
| Degree of parallelism                   | 1       | How many items should be downloaded in parallel. Default is set to 1 to go easy on the servers.                             |
| Ignore themes that overlap with episode | true    | Some shows transition into their themes. Use this setting to filter those themes out.                                       |
| Ignore OP themes                        | false   | Use this to control whether OPs are considered.                                                                             |
| Ignore ED themes                        | false   | Use this to control whether EDs are considered.                                                                             |
| (Audio) Fetch type                      | Single  | Whether to fetch no audio, *all* themes (`theme-music/{slug}.mp3`) or just one that is picked by some factors. (`theme.mp3`) |
| (Audio) Volume                          | 0.5     | The volume that should be used for downloaded themes. (The volume will be baked in.)                                        |
| (Video) Fetch type                      | None    | Whether to fetch no video, *all* themes (`backdrops/{slug}.webm`) or just one that is picked by some factors.               |
| (Video) Volume                          | 0       | The volume for videos. 0 will result with the audio channel being removed.                                                  |
