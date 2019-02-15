namespace MusicCat.Metadata
{
	public class Game
	{
		public string id { get; set; }
		public string title { get; set; }
		public string series { get; set; }
		public string year { get; set; }
		public string[] platform { get; set; }
		public bool is_fanwork { get; set; }
	}
}
