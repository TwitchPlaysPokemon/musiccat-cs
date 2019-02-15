using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MusicCat.Metadata
{
	public class MetadataObj
	{
		public string id { get; set; }
		public string title { get; set; }
		public string series { get; set; } = null;
		public string year { get; set; }
		public string[] platform { get; set; }
		public bool is_fanwork { get; set; } = false;

		public List<Song> songs { get; set; }
	}

	public class Song
	{
		public string id { get; set; }
		public string path { get; set; }
		public string title { get; set; }
		[JsonConverter(typeof(StringEnumConverter))]
		public SongType[] types { get; set; }
		public float[] ends { get; set; } = null;
		public string[] tags { get; set; } = null;
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
