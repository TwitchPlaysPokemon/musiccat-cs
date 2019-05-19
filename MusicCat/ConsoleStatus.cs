using System;
using System.Collections.Generic;
using System.Text;

namespace MusicCat
{
	public class ConsoleStatus
	{
		public int volume { get; }
		public int shuffle { get; }
		public int repeat { get; }
		public string album { get; }
		public string artist { get; }
		public string title { get; }
		public string filename { get; }
		public string length { get; }
		public long lengthms { get; }
		public string bitrate { get; }
		public float position { get; }
	}
}
