namespace MusicCat.Model;

public record Game(
    string Id,
    string Title,
    string[] Platform,
    int? Year,
    string[]? Series,
    bool IsFanwork
);