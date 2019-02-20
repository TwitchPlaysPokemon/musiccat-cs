using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MusicCat.Metadata
{
	public class Song
	{
		public string id { get; set; }
		public string path { get; set; }
		public string title { get; set; }
		[JsonProperty("types", ItemConverterType = typeof(StringEnumConverter))]
		public SongType[] types { get; set; }
		public float[] ends { get; set; } = null;
		public string[] tags { get; set; } = null;
		public Game game { get; set; }
	}

	public enum SongType
	{
		result,
		betting,
		battle,
		warning,
		@break
	}
}
