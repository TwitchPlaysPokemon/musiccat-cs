using System;
using System.Collections.Generic;
using System.Text;

namespace MusicCat.Metadata
{
	public class Metadata
	{
		public List<Song> songs;
		public string id { get; set; }
		public string title { get; set; }
		public string series { get; set; }
		public string year { get; set; }
		public string[] platform { get; set; }
		public bool is_fanwork { get; set; }
	}
}
