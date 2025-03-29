using Microsoft.Extensions.Configuration;
using MusicCat.Players;

namespace MusicCat;

public class Config
{
	public string? MusicBaseDir { get; init; }
	public string? SongFileDir { get; init; }
	public string? WinampPath { get; init; }
	public AjaxAMPConfig AjaxAMP { get; init; } = new();

	public static Config LoadFromConfiguration(IConfiguration configuration)
	{
		var config = new Config();
		configuration.GetSection("MusicCat").Bind(config);
		return config;
	}
}
