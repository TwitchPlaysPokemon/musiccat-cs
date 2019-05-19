using System;
using System.Xml.Serialization;

namespace MusicCat
{
	[Serializable, XmlRoot("console")]
	public class console
	{
		[XmlElement("volume")]
		public int volume { get; }
		[XmlElement("shuffle")]
		public int shuffle { get; }
		[XmlElement("repeat")]
		public int repeat { get; }
		[XmlElement("album")]
		public string album { get; }
		[XmlElement("artist")]
		public string artist { get; }
		[XmlElement("title")]
		public string title { get; }
		[XmlElement("filename")]
		public string filename { get; }
		[XmlElement("length")]
		public string length { get; }
		[XmlElement("lengthms")]
		public long lengthms { get; }
		[XmlElement("bitrate")]
		public string bitrate { get; }
		[XmlElement("position")]
		public float position { get; }
	}
}
