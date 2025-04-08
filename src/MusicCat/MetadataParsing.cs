using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using MusicCat.Model;
using VYaml.Serialization;

namespace MusicCat;

public record MetadataLoadResult(
    IList<string> Warnings,
    IDictionary<string, Song> Songs,
    ISet<string> UnusedSongFiles,
    TimeSpan DurationReadMetadata,
    TimeSpan DurationReadSongFiles);

public partial class MetadataParsing
{
    public static async Task<MetadataLoadResult> LoadMetadata(
        string musiclibraryPath,
        string? songfilesPath = null)
    {
        songfilesPath ??= musiclibraryPath;

        var warningsCollector = new ConcurrentQueue<string>();
        HashSet<string> existingSongFiles = [];
        TimeSpan durationReadMetadata = TimeSpan.Zero;
        TimeSpan durationReadSongFiles = TimeSpan.Zero;

        var songFilesTask = Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            var songFilePaths = Directory
                .EnumerateFiles(songfilesPath, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(songfilesPath, f));
            foreach (string songFile in songFilePaths)
                existingSongFiles.Add(songFile);
            durationReadSongFiles = stopwatch.Elapsed;
        });
        var metadataFilesTask = Task.Run(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            var songs = await ParseEntireDirectory(warningsCollector, musiclibraryPath);
            durationReadMetadata = stopwatch.Elapsed;
            return songs;
        });
        await Task.WhenAll(songFilesTask, metadataFilesTask);

        var songs = await metadataFilesTask;
        Dictionary<string, Song> songsDict = new();
        foreach (Song song in songs)
            if (!songsDict.TryAdd(song.Id, song))
                warningsCollector.Enqueue(
                    $"{song.Id}: Song ID is used multiple times: {song.Path} vs {songsDict[song.Id].Path}");

        foreach (Song song in songs)
        {
            if (!existingSongFiles.Contains(song.Path))
            {
                warningsCollector.Enqueue($"Missing song file: {song.Path}");
                songsDict.Remove(song.Id);
            }
        }

        var usedSongFiles = songs.Select(s => s.Path).ToHashSet();
        HashSet<string> ignoreUnrelated = [".txt", ".png", ".jpg", ".ini", ".sh"];
        HashSet<string> ignoreSoundFonts = [".usflib", ".gsflib", ".2sflib", ".ncsflib"];
        HashSet<string> unusedSongFiles = existingSongFiles
            .Except(usedSongFiles)
            .Where(file =>
            {
                string extension = Path.GetExtension(file).ToLowerInvariant();
                return !ignoreUnrelated.Contains(extension) &&
                       !ignoreSoundFonts.Contains(extension);
            })
            .ToHashSet();

        return new MetadataLoadResult(
            Warnings: warningsCollector.ToList(),
            Songs: songsDict,
            UnusedSongFiles: unusedSongFiles,
            DurationReadMetadata: durationReadMetadata,
            DurationReadSongFiles: durationReadSongFiles);
    }

    private static async Task<IReadOnlyCollection<Song>> ParseEntireDirectory(
        ConcurrentQueue<string> warningsCollector, string musiclibraryPath)
    {
        IEnumerable<string> metadataFiles =
            Directory.EnumerateFiles(musiclibraryPath, "metadata.yaml", SearchOption.AllDirectories);

        var songs = new ConcurrentBag<Song>();

        await Parallel.ForEachAsync(metadataFiles, async (filename, cancellationToken) =>
        {
            try
            {
                var songsOfFile = await ParseSingleFile(warningsCollector, filename, musiclibraryPath);
                songsOfFile.ForEach(songs.Add);
            }
            catch (Exception ex)
            {
                warningsCollector.Enqueue(
                    $"Failed reading file '{Path.GetRelativePath(musiclibraryPath, filename)}': {ex.Message}");
            }
        });

        return songs;
    }

    private static async Task<List<Song>> ParseSingleFile(
        ConcurrentQueue<string> warningsCollector, string filename, string musiclibraryPath)
    {
        await using FileStream stream = File.OpenRead(filename);
        // We're reading it dynamically instead of into a fixed type for mainly two reasons:
        // - Some fields are liberally typed, e.g. "platform" can be a single string or a list.
        // - VYaml's error messages aren't very specific, but we want precise error messages.
        //   Why VYaml and not YamlDotNet? Mostly just because it's a bit faster and more lightweight.
        var gameYaml = await YamlSerializer.DeserializeAsync<IDictionary<object, object>>(stream);

        (Game game, IEnumerable<object> songs) = ParseGame(gameYaml);

        string relativeFileName = Path.GetRelativePath(musiclibraryPath, filename);

        return songs.SelectMany<object, Song>(songObj =>
        {
            try
            {
                Song song = ParseSong(songObj, game, Path.GetDirectoryName(relativeFileName) ?? "");
                return [song];
            }
            catch (Exception ex)
            {
                warningsCollector.Enqueue(
                    $"Failed reading a song of game {game.Id}, file '{relativeFileName}': {ex.Message}");
                return [];
            }
        }).ToList();
    }

    private static readonly HashSet<string> KnownGameKeys =
        ["id", "title", "platform", "year", "series", "is_fanwork", "songs"];

    private static (Game, IEnumerable<object>) ParseGame(IDictionary<object, object> gameYaml)
    {
        HashSet<object> unknownGameKeys = gameYaml.Keys.Except(KnownGameKeys).ToHashSet();
        if (unknownGameKeys.Count != 0)
            throw new ArgumentException("Unknown fields in game: " + string.Join(", ", unknownGameKeys));

        string context = "game";
        string gameId = GetAs<string>(gameYaml, "id", context);
        string gameTitle = GetAs<string>(gameYaml, "title", context);
        if (string.IsNullOrWhiteSpace(gameTitle))
            throw new ArgumentException($"Game title of game {gameId} must not be empty");
        string[] platforms = GetAsEnumerable<string>(gameYaml, "platform", context).ToArray();
        int? year = GetAsYearMaybeUnreleased(gameYaml);
        string[]? series = GetAsEnumerableOptional<string>(gameYaml, "series", context)?.ToArray();
        bool fanwork = GetAsOptional<bool>(gameYaml, "is_fanwork", context);
        IEnumerable<object> songs = GetAsEnumerable<object>(gameYaml, "songs", context, allowScalarAs1Elem: false);

        var game = new Game(gameId, gameTitle, platforms, year, series, fanwork);
        return (game, songs);
    }

    private static readonly HashSet<string> KnownSongKeys = ["id", "title", "type", "ends", "tags", "path"];

    private static Song ParseSong(object songObj, Game game, string gamePathRelativeToLibrary)
    {
        if (songObj is not IDictionary<object, object> songYaml)
            throw new ArgumentException("song must be an object, but was " + songObj.GetType());

        string songId = GetAs<string>(songYaml, "id", context: "<first encountered song>");
        string context = "song " + songId;

        HashSet<string> unknownSongKeys = songYaml.Keys.Cast<string>().Except(KnownSongKeys).ToHashSet();
        if (unknownSongKeys.Count != 0)
            throw new ArgumentException($"Unknown fields in song {songId}: " + string.Join(", ", unknownSongKeys));

        string songTitle = GetAs<string>(songYaml, "title", context);
        if (string.IsNullOrWhiteSpace(songTitle))
            throw new ArgumentException($"Song title of song {songId} must not be empty");
        HashSet<SongType> types = GetAsEnumerable<string>(songYaml, "type", context).Select(ParseSongType).ToHashSet();
        ISet<TimeSpan>? ends = GetAsEnumerableOptional<string>(songYaml, "ends", context)
            ?.Select(MinutesSecondsStringToTimeSpan).ToHashSet();
        ISet<string>? tags = GetAsEnumerableOptional<string>(songYaml, "tags", context)?.ToHashSet();
        string pathRelativeToGame = GetAs<string>(songYaml, "path", context);
        string path = Path.Combine(gamePathRelativeToLibrary, pathRelativeToGame);
        return new Song(songId, songTitle, types, ends, tags, game, path);
    }

    private static SongType ParseSongType(string str)
    {
        if (!Enum.TryParse(str, ignoreCase: true, out SongType songType))
            throw new ArgumentException($"Song has unrecognized song type: {str}");
        return songType;
    }

    private static int? GetAsYearMaybeUnreleased(IDictionary<object, object> gameYaml)
    {
        if (gameYaml.TryGetValue("year", out var yearObj) &&
            "UNRELEASED".Equals(yearObj as string, StringComparison.OrdinalIgnoreCase))
            return null;
        return GetAs<int>(gameYaml, "year", context: "game");
    }

    private static T? GetAsOptional<T>(IDictionary<object, object> dict, string key, string context)
    {
        if (!dict.TryGetValue(key, out var obj))
            return default;
        if (obj is not T value)
            throw new ArgumentException(
                $"Field '{key}' of {context} must be of type {typeof(T).Name}, but was {obj.GetType().Name}");
        return value;
    }

    private static T GetAs<T>(IDictionary<object, object> dict, string key, string context)
    {
        return GetAsOptional<T>(dict, key, context)
               ?? throw new ArgumentException($"Missing field '{key}' of {context}");
    }

    private static IEnumerable<T>? GetAsEnumerableOptional<T>(IDictionary<object, object> dict, string key,
        string context, bool scalarAs1Elem = true)
    {
        if (!dict.TryGetValue(key, out var obj))
            return null;
        if (scalarAs1Elem && obj is not IEnumerable<object> && obj is T scalar) return [scalar];
        var enumerable = obj as IEnumerable<object> ?? throw new ArgumentException(
            $"Field '{key}' of {context} must be of type {typeof(T).Name} or a list of such, but was {obj.GetType().Name}");
        return enumerable.Select(valueObj =>
        {
            if (valueObj is not T value)
                throw new ArgumentException(
                    $"{context}: All items of {key} must be of type {typeof(T).Name}, but one was {valueObj.GetType().Name}");
            return value;
        });
    }

    private static IEnumerable<T> GetAsEnumerable<T>(IDictionary<object, object> dict, string key, string context,
        bool allowScalarAs1Elem = true)
    {
        return GetAsEnumerableOptional<T>(dict, key, context, allowScalarAs1Elem)
               ?? throw new ArgumentException($"Missing field '{key}' of {context}");
    }

    private static TimeSpan MinutesSecondsStringToTimeSpan(string str)
    {
        Match match = MinutesSecondsTimeSpanRegex().Match(str);
        if (!match.Success)
            throw new ArgumentException($"End of song does not match format 'mm:ss': {str}");
        return TimeSpan.FromSeconds(
            int.Parse(match.Groups[1].Value) * 60 + int.Parse(match.Groups[2].Value));
    }

    [GeneratedRegex(@"^(\d+):(\d\d)$")]
    private static partial Regex MinutesSecondsTimeSpanRegex();
}