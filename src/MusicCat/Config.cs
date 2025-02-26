﻿#nullable disable

using MusicCat.Players;
using Newtonsoft.Json;

namespace MusicCat;

public class Config
{
	public int HttpPort { get; set; }
	public string MusicBaseDir { get; set; }
	public string SongFileDir { get; set; } = null;
	public string LogDir { get; set; }
	public string WinampPath { get; set; }
	public AjaxAMPConfig AjaxAMPConfig { get; set; }

	public static Config DefaultConfig => new()
	{
		HttpPort = 7337,
		MusicBaseDir = "D:\\Music",
		LogDir = "D:\\Music",
		WinampPath = "C:\\Program Files (x86)\\Winamp\\winamp.exe",
		AjaxAMPConfig = new AjaxAMPConfig
		{
			BaseUrl = "http://localhost:5151",
			MaxVolume = 2.0f
		}
	};

	public static Config ParseConfig(out FileStream logStream, out StreamWriter logWriter)
	{
		string configJson = File.ReadAllText("MusicCatConfig.json");
		Config config = JsonConvert.DeserializeObject<Config>(configJson);
		if (!string.IsNullOrWhiteSpace(config.LogDir) &&
		    !File.Exists(Path.Combine(config.LogDir, "MusicCatLog.txt")))
		{
			logStream = File.Create(Path.Combine(config.LogDir, "MusicCatLog.txt"));
			logWriter = new StreamWriter(logStream);
		}
		else if (!string.IsNullOrWhiteSpace(config.LogDir))
		{
			logStream = File.OpenWrite(Path.Combine(config.LogDir, "MusicCatLog.txt"));
			logWriter = new StreamWriter(logStream);
		}
		else
		{
			logStream = null;
			logWriter = null;
		}
		return config;
	}
}