using Fastenshtein;
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
            logger.LogInformation("Loaded {NumLoaded} songs. Metadata loading took {MetadataLoadTime}ms, " +
                                  "song file checking took {SongfileCheckingTime}ms. " +
                                  "{NumWarnings} warnings occurred and {NumUnusedSongfiles} song files appear unused.",
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

    public async Task<IEnumerable<Song>> List(SongType? songType, string? gameId, string? tag, int? sample)
    {
        var songsDict = await _songs.Task;
        var resultEnumerable = songsDict.Values.AsEnumerable();
        if (songType != null)
            resultEnumerable = resultEnumerable.Where(song => song.Types.Contains(songType.Value));
        if (gameId != null)
            resultEnumerable = resultEnumerable.Where(song => song.Game.Id == gameId);
        if (tag != null)
            resultEnumerable = resultEnumerable.Where(song => song.Tags != null && song.Tags.Contains(tag));
        
        return sample == null 
            ? resultEnumerable
            : new Random().GetItems(resultEnumerable.ToArray(), length: sample.Value);
    }

    public record SearchResult(Song Song, float MatchRatio);

    public async Task<List<SearchResult>> Search(
        string[] keywords,
        string? requiredTag = null,
        int limit = 100,
        float cutoff = 0.3f)
    {
        keywords = keywords.Select(k => k.ToLowerInvariant()).ToArray();
        var keywordMatchers = keywords
            .Select(k => new Levenshtein(k))
            .ToArray();
        var songsDict = await _songs.Task;
        // It might sound a bit wasteful to iterate over the entire library for each search,
        // but this just takes a couple of hundred milliseconds at most, so it's good enough.
        // This could be improved by e.g. building a trigram index or something I suppose. 
        return songsDict.Values
            // .AsParallel() // We're using Fastenshtein instances, which are not thread safe, but we're fast anyway!
            .Where(song => requiredTag == null || (song.Tags != null && song.Tags.Any(tag => keywords.Contains(tag))))
            .Select(song =>
            {
                string[] keywordsSong = song.Title.ToLowerInvariant().Split(' ');
                string[] keywordsGame = song.Game.Title.ToLowerInvariant().Split(' ');

                float matchRatio = keywordMatchers.Average(matcher =>
                {
                    float similarityPercentageSong = keywordsSong
                        .Max(kw => 1f - matcher.DistanceFrom(kw) / (float)Math.Max(kw.Length, matcher.StoredLength));
                    float similarityPercentageGame = keywordsGame
                        .Max(kw => 1f - matcher.DistanceFrom(kw) / (float)Math.Max(kw.Length, matcher.StoredLength));

                    return Math.Max(similarityPercentageSong, similarityPercentageGame * 0.9f);
                });

                return new SearchResult(song, matchRatio);
            })
            .Where(match => match.MatchRatio >= cutoff)
            .OrderByDescending(x => x.MatchRatio)
            .ThenBy(x => x.Song.Title) // make order deterministic for ratio ties: just sort alphabetically
            .Take(limit)
            .ToList();
    }
}