#nullable disable

using System.Xml.Serialization;

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
	// Let's not parse this, as we don't need it and it fails to parse for some songs.
	// E.g. if we try to play edda_castle, the XML contains "1.#INF", which just doesn't compute.
	// [XmlElement(ElementName = "position")]
	// public float Position { get; set; }
}