using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MusicCat.Players;

namespace MusicCat;

public class Config
{
	public string? MusicBaseDir { get; init; }
	public string? SongFileDir { get; init; }
	public string? WinampPath { get; init; }
	public AjaxAMPConfig AjaxAMP { get; init; } = new();
	public bool LogSongFileWarnings { get; init; } = true;
	public DiscordLoggingConfig? DiscordLogging { get; init; } = null;

	public static Config LoadFromConfiguration(IConfiguration configuration)
	{
		var config = new Config();
		configuration.GetSection("MusicCat").Bind(config);
		return config;
	}
}

public sealed class DiscordLoggingConfig
{
	public ulong WebhookId { get; init; } = 0L;
	public string WebhookToken { get; init; } = "";
	public LogLevel MinLogLevel { get; init; } = LogLevel.Warning;
}
