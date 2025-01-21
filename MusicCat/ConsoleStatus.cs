using System;
using System.Xml.Serialization;

#nullable disable

namespace MusicCat;

[XmlRoot(ElementName = "console")]
public class ConsoleStatus
{
	[XmlElement(ElementName = "volume")]
	public int Volume { get; set; }
	[XmlElement(ElementName = "shuffle")]
	public int Shuffle { get; set; }
	[XmlElement(ElementName = "repeat")]
	public int Repeat { get; set; }
	[XmlElement(ElementName = "album")]
	public string Album { get; set; }
	[XmlElement(ElementName = "artist")]
	public string Artist { get; set; }
	[XmlElement(ElementName = "title")]
	public string Title { get; set; }
	[XmlElement(ElementName = "filename")]
	public string Filename { get; set; }
	[XmlElement(ElementName = "length")]
	public string Length { get; set; }
	[XmlElement(ElementName = "lengthms")]
	public long LengthMs { get; set; }
	[XmlElement(ElementName = "bitrate")]
	public string Bitrate { get; set; }
	[XmlElement(ElementName = "position")]
	public float Position { get; set; }
}