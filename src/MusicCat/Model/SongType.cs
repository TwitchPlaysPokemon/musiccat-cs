using System.Diagnostics.CodeAnalysis;

namespace MusicCat.Model;

// Enum members are named in lowercase on purpose until https://github.com/dotnet/aspnetcore/issues/48346 is fixed:
// Right now, there's no easy way to make ASP.NET parameter enum parsing case-insensitive,
// and we don't want C# naming conventions (PascalCase) in our API.
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum SongType
{
    result,
    betting,
    battle,
    warning,
    @break
}
