using System;
using System.Collections.Generic;
using System.Linq;
using ApiListener;
using MusicCat.Metadata;

namespace MusicCat.API;

public class MetadataApi : ApiProvider
{
	public override IEnumerable<ApiCommand> Commands => new List<ApiCommand> {
		new ApiCommand("Count", args=>MetadataCommands.Count(args.Count() == 1 ? args.ToArray()[0] : null).Result, new List<ApiParameter>{new ApiParameter("Category", "string", true)}, "Gets the number of songs in the library. Returns an int"),
		new ApiCommand("Search", args=>MetadataCommands.Search(args.ToArray()).Result, new List<ApiParameter>{ new ApiParameter("Keywords", "string[]") }, "Searches through the metadata to provide songs with the closest match. Returns a List<Tuple<Song, float match>>"),
		new ApiCommand("GetRandomSong", args=>MetadataCommands.GetRandomSong().Result, new List<ApiParameter>{ new ApiParameter("MinTime", "int", true) }, "Returns a random song across all categories. Returns a Song"),
		new ApiCommand("GetRandomSongBy", args=>MetadataCommands.GetRandomSongBy(args.ToArray()).Result, new List<ApiParameter>{ new ApiParameter("FilterType", "String (one of Tag, Category, or Game)"), new ApiParameter("FilterValue", "string") }, "Returns a random song that fits the filter. Returns a Song")
	};

	public MetadataApi (Action<ApiLogMessage> logger)
		: base(logger)
	{ }
}