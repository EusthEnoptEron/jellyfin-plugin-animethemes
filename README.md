# AnimeThemes for Jellyfin

Experimental Plugin for [Jellyfin](https://jellyfin.org/) that fetches anime themes (OPs and EDs)
from [animethemes.moe](https://animethemes.moe).

**Since the AniDB ID is used to find the matching themes, you'll need to install the AniDB plugin.**

## Configuration

| Configuration           | Default | Description                                                                                                        |
|-------------------------|---------|--------------------------------------------------------------------------------------------------------------------|
| Volume                  | 0.5     | The volume that should be used for downloaded themes. (The volume will be baked in.)                               |
| Degree of parallelism   | 1       | How many items should be downloaded in parallel. Default is set to 1 to go easy on the servers.                    |
| Ignore themes with text | true    | Some shows transition into their themes. Use this setting to filter those themes out.                              |
| Ignore OP themes        | false   | Use this to control whether OPs are considered.                                                                    |
| Ignore ED themes        | false   | Use this to control whether EDs are considered.                                                                    |
| Fetch all themes        | false   | Whether to fetch *all* themes (`theme-music/{slug}.mp3`) or just one that is picked by some factors. (`theme.mp3`) |
