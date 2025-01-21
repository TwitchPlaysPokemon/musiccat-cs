using ApiListener;
using MusicCat.Metadata;

namespace MusicCat.API;

public class MetadataApi(Action<ApiLogMessage> logger) : ApiProvider(logger)
{
	public override IEnumerable<ApiCommand> Commands => new List<ApiCommand> {
		new("Count", args=>MetadataCommands.Count(args.Count() == 1 ? args.ToArray()[0] : null).Result, new List<ApiParameter>{new("Category", "string", true)}, "Gets the number of songs in the library. Returns an int"),
		new("Search", args=>MetadataCommands.Search(args.ToArray()).Result, new List<ApiParameter>{ new("Keywords", "string[]") }, "Searches through the metadata to provide songs with the closest match. Returns a List<Tuple<Song, float match>>"),
		new("GetRandomSong", args=>MetadataCommands.GetRandomSong().Result, new List<ApiParameter>{ new("MinTime", "int", true) }, "Returns a random song across all categories. Returns a Song"),
		new("GetRandomSongBy", args=>MetadataCommands.GetRandomSongBy(args.ToArray()).Result, new List<ApiParameter>{ new("FilterType", "String (one of Tag, Category, or Game)"), new("FilterValue", "string") }, "Returns a random song that fits the filter. Returns a Song")
	};
}