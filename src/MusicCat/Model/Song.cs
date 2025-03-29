namespace MusicCat.Model;

public record Song(
    string Id,
    string Title,
    ISet<SongType> Types,
    ISet<TimeSpan>? Ends,
    ISet<string>? Tags,
    Game Game,
    string Path
);