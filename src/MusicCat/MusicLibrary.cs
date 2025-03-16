using Microsoft.Extensions.Logging;
using MusicCat.Model;

namespace MusicCat;

public class MusicLibrary(
    ILogger<MusicLibrary> logger,
    string musiclibraryPath,
    string? songfilesPath = null
)
{
    // Put behind a task completion source, so the songs can get loaded lazily.
    // This way MusicCat boots faster and can immediately start serving requests.
    // Acceptable downside: Every operation is async and may wait until the metadata is loaded.
    private TaskCompletionSource<IDictionary<string, Song>> _songs = new();
    
    /// Loads the music library.
    /// If it was already loaded, removes the currently loaded data and loads again from disk.
    public async Task<IList<string>> Load()
    {
        _songs = new TaskCompletionSource<IDictionary<string, Song>>();
        try
        {
            var loadResult = await MetadataParsing.LoadMetadata(musiclibraryPath, songfilesPath);
            logger.LogInformation("Loaded {} songs. Metadata loading took {}ms, song file checking took {}ms. " +
                                  "{} warnings occurred and {} song files appear unused.",
                loadResult.Songs.Count,
                loadResult.DurationReadMetadata.TotalMilliseconds, loadResult.DurationReadSongFiles.TotalMilliseconds,
                loadResult.Warnings.Count, loadResult.UnusedSongFiles.Count);
            _songs.SetResult(loadResult.Songs);
            return loadResult.Warnings;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error loading metadata");
            _songs.SetException(e);
            return [];
        }
    }

    /// Practically does the same as <see cref="Load"/>, but instead of keeping the loaded data,
    /// just returns any warnings/errors that occurred during loading.
    public async Task<IList<string>> Verify(bool reportUnusedSongFiles = false)
    {
        try
        {
            var loadResult = await MetadataParsing.LoadMetadata(musiclibraryPath, songfilesPath);
            List<string> result = [..loadResult.Warnings];
            if (reportUnusedSongFiles)
                foreach (string unusedSongFile in loadResult.UnusedSongFiles)
                    result.Add("Unused song file: " + unusedSongFile);
            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error verifying metadata");
            return ["Error verifying metadata (details in log): " + e.Message];
        }
    }

    public async Task<int> Count(SongType? songType)
    {
        var songs = await _songs.Task;
        return songType == null
            ? songs.Count
            : songs.Values.Count(song => song.Types.Contains(songType.Value));
    }

    public async Task<Song?> Get(string id)
    {
        var songsDict = await _songs.Task;
        return songsDict.TryGetValue(id, out var song) ? song : null;
    }
}