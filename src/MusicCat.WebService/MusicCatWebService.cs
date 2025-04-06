using MusicCat.Model;
using MusicCat.Players;

namespace MusicCat.WebService;

public static class MusicCatWebService
{
    public static MusicLibrary GetMusicLibrary(ILogger<MusicLibrary> logger, Config config)
    {
        var musicLibrary = new MusicLibrary(
            logger,
            musiclibraryPath: config.MusicBaseDir!, // TODO should not be nullable
            songfilesPath: config.SongFileDir);
        var loadTask = musicLibrary.Load(); // Start loading asynchronously, we don't need to wait for this to finish.
        _ = loadTask.ContinueWith(task => // But if it fails, we want to know about it
        {
            if (task.IsFaulted) logger.LogError(task.Exception, "Music Library load faulted");
        });
        return musicLibrary;
    }

    public static void AddMusicCatEndpoints(MusicLibrary musicLibrary, WebApplication app)
    {
        app.MapGet("/musiclibrary/verify", async (bool reportUnusedSongFiles = false) =>
            {
                IList<string> warnings = await musicLibrary.Verify(reportUnusedSongFiles);
                return string.Join('\n', warnings);
            }).WithDescription("Like /reload, but a dry-run. So it just returns all problems that would occur")
            .WithOpenApi();

        app.MapPost("/musiclibrary/reload", async () =>
            {
                IList<string> warnings = await musicLibrary.Load();
                return string.Join('\n', warnings);
            }).WithDescription("Reloads the entire music library from disk, returning all problems that occurred")
            .WithOpenApi();

        app.MapGet("/musiclibrary/count", async (string? category) =>
            {
                if (category == null)
                    return await musicLibrary.Count(null);
                if (!Enum.TryParse(category, ignoreCase: true,
                        out SongType songType)) // parse manually for case-insensitivity
                    throw new BadHttpRequestException("Unrecognized song type: " + category);
                return await musicLibrary.Count(songType);
            }).WithDescription("Counts all currently enabled songs in the library, optionally filtered to one type")
            .WithOpenApi();
    }

    public static void AddPlayerEndpoints(IPlayer player, WebApplication app)
    {
        app.MapPost("/player/launch",
            async () => await player.Launch());
        app.MapPost("/player/play",
            async () => await player.Play());
        app.MapPost("/player/pause",
            async () => await player.Pause());
        app.MapPost("/player/stop",
            async () => await player.Stop());

        app.MapPost("/player/play/{id}",
            async (string id) => await player.PlayID(id));
        app.MapPost("/player/play-file/{filename}",
            async (string filename) => await player.PlayFile(filename));

        app.MapGet("/player/volume",
            async () => await player.GetVolume());
        app.MapPut("/player/volume/{level:float}", async Task (float level) =>
        {
            if (level is < 0 or > 1)
                throw new BadHttpRequestException("player volume level must be between 0 and 1");
            await player.SetVolume(level);
        });

        app.MapGet("/player/position",
            async () => await player.GetPosition());
        app.MapPut("/player/position/{pos:float}", async Task (float pos) =>
        {
            if (pos is < 0 or > 1)
                throw new BadHttpRequestException("player seek position must be between 0 and 1");
            await player.SetPosition(pos);
        });
    }
}