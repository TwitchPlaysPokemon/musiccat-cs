using MusicCat.Players;

namespace MusicCat;

public class Config
{
	public int HttpPort { get; init; }
	public string? MusicBaseDir { get; init; }
	public string? SongFileDir { get; init; }
	public string? LogPath { get; init; }
	public string? WinampPath { get; init; }
	public AjaxAMPConfig? AjaxAMPConfig { get; init; }
}