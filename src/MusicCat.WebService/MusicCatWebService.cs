using Microsoft.AspNetCore.Mvc;
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
        const string openapiTag = "Music Library";

        app.MapGet("/musiclibrary/verify", async (bool reportUnusedSongFiles = false) =>
            {
                IList<string> warnings = await musicLibrary.Verify(reportUnusedSongFiles);
                return string.Join('\n', warnings);
            }).WithDescription("Like /reload, but a dry-run. So it just returns all problems that would occur")
            .WithOpenApi().WithTags(openapiTag);

        app.MapPost("/musiclibrary/reload", async () =>
            {
                IList<string> warnings = await musicLibrary.Load();
                return string.Join('\n', warnings);
            }).WithDescription("Reloads the entire music library from disk, returning all problems that occurred")
            .WithOpenApi().WithTags(openapiTag);

        app.MapGet("/musiclibrary/count", async (SongType? category) =>
                await musicLibrary.Count(category))
            .WithDescription("Counts all currently enabled songs in the library, optionally filtered to one type")
            .WithOpenApi().WithTags(openapiTag);

        app.MapGet("/musiclibrary/songs/{id}", async (string id) => await musicLibrary.Get(id))
            .WithDescription("Finds a song by its ID, returning null if not found")
            .WithOpenApi().WithTags(openapiTag);

        app.MapGet("/musiclibrary/songs",
                async (SongType? category, string? gameId, string? tag, int? sample) =>
                    await musicLibrary.List(category, gameId, tag, sample))
            .WithDescription(
                "Returns all currently enabled songs in the library, optionally filtered to one type and/or one " +
                "gameId and/or one tag. If 'sample' is provided, only returns that many songs, chosen at random.")
            .WithOpenApi().WithTags(openapiTag);

        app.MapGet("/musiclibrary/search", async (string[] keywords, string? requiredTag, int limit = 100) =>
                await musicLibrary.Search(keywords, requiredTag, limit))
            .WithDescription("Searches through songs in the library, returning them ordered by relevance descending")
            .WithOpenApi().WithTags(openapiTag);
    }

    public static void AddPlayerEndpoints(IPlayer player, WebApplication app)
    {
        const string openapiTag = "Player";

        app.MapPost("/player/launch", async () => await player.Launch())
            .WithDescription("Launches WinAMP, if not already running").WithTags(openapiTag);
        app.MapPost("/player/play", async () => await player.Play())
            .WithDescription("Starts or resumes playing the current song").WithTags(openapiTag);
        app.MapPost("/player/pause", async () => await player.Pause())
            .WithDescription("Pauses the currently playing song").WithTags(openapiTag);
        app.MapPost("/player/stop", async () => await player.Stop())
            .WithDescription("Stops the currently playing song").WithTags(openapiTag);

        app.MapPost("/player/play/{id}", async (string id) => await player.PlayID(id))
            .WithDescription("Plays a song by its song ID").WithTags(openapiTag);
        app.MapPost("/player/play-file/{filename}", async (string filename) => await player.PlayFile(filename))
            .WithDescription("Plays a song by its filename").WithTags(openapiTag);

        app.MapGet("/player/volume", async () => await player.GetVolume())
            .WithDescription("Gets WinAMP's current volume as a float between 0 and the configured max volume")
            .WithTags(openapiTag);
        app.MapPut("/player/volume", async Task ([FromBody] float level) =>
            {
                if (level < 0 || level > player.MaxVolume)
                    throw new BadHttpRequestException("player volume level must be between 0 and " + player.MaxVolume);
                await player.SetVolume(level);
            })
            .WithDescription("Sets WinAMP's volume to a value between 0 and the configured max volume")
            .WithTags(openapiTag);

        app.MapGet("/player/position", async () => await player.GetPosition())
            .WithDescription("Gets WinAMP's current playing position as a float ranging from 0 to 1")
            .WithTags(openapiTag);
        app.MapPut("/player/position", async Task ([FromBody] float pos) =>
            {
                if (pos is < 0 or > 1)
                    throw new BadHttpRequestException("player seek position must be between 0 and 1");
                await player.SetPosition(pos);
            })
            .WithDescription("Sets WinAMP's current position as a float ranging from 0 to 1").WithTags(openapiTag);
    }
}