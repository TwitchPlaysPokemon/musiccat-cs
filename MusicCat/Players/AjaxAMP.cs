using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Serialization;
using ApiListener;
using MusicCat.Metadata;
using static MusicCat.Metadata.MetadataStore;

namespace MusicCat.Players;

public class AjaxAMP : IPlayer
{
	private HttpClient httpClient;
	private Process winAmp;
	private float MaxVolume;

	public AjaxAMP(AjaxAMPConfig config)
	{
		httpClient = new HttpClient
		{
			BaseAddress = new Uri(config.BaseUrl)
		};
		MaxVolume = config.MaxVolume;
	}

	private Task<string> SendCommand(string command, Dictionary<string, string> args = null, bool post = false)
	{
		var argsDigest = string.Join("&", (args ?? new Dictionary<string, string>()).Keys.Select(k => $"{Uri.EscapeDataString(k)}={Uri.EscapeDataString(args[k])}"));
		var request = new HttpRequestMessage(post ? HttpMethod.Post : HttpMethod.Get, command + (post || args == null ? "" : "?" + argsDigest));
		if (post)
		{
			request.Content = new StringContent(argsDigest);
		}
		return httpClient.SendAsync(request).Result.Content.ReadAsStringAsync();
	}

	private Task<string> Post(string command, Dictionary<string, string> args = null) => SendCommand(command, args, true);

	private Task<string> Get(string command, Dictionary<string, string> args = null) => SendCommand(command, args);

	public async Task<float> GetPosition() => float.Parse(await Get("getposition"));

	public async Task<float> GetVolume() => float.Parse(await Get("getvolume")) / 255f * MaxVolume;

	private async Task<ConsoleStatus> GetStatus()
	{
		ConsoleStatus status;
		var serializer = new XmlSerializer(typeof(ConsoleStatus));
		string serialized = await Get("consolestatus.xml");
		using (StringReader reader = new StringReader(serialized))
		{
			status = serializer.Deserialize(reader) as ConsoleStatus;
		}
		return status;
	}


	public async Task Launch()
	{
		if (Process.GetProcessesByName("Winamp").Length > 0)
			return;

		if (string.IsNullOrEmpty(Listener.Config.WinampPath))
			throw new ApiError("Winamp path in the config is not set.");

		if (!File.Exists(Listener.Config.WinampPath))
			throw new ApiError("There is no file at the given path.");

		winAmp = new Process { StartInfo = { FileName = Listener.Config.WinampPath } };
		winAmp.Start();

		bool flag = false;
		while (!flag)
		{
			try
			{
				await Task.Delay(1000);
				string active = (await GetPosition()).ToString(CultureInfo.InvariantCulture);
				if (!string.IsNullOrWhiteSpace(active))
					flag = true;
			}
			catch { }
		}
	}

	public Task Pause() => Post("pause");

	public Task Play() => Post("play");

	/// <summary>
	/// Tells the music player to play a file
	/// </summary>
	/// <param name="filename">Absolute path of file (Drive Letter must be uppercased. AjaxAMP is very very picky)</param>
	/// <returns></returns>
	public async Task PlayFile(string filename)
	{
		await Post("playfile", new Dictionary<string, string>
		{
			["filename"] = filename,
			["title"] = filename
		});

		float position1 = await GetPosition();

		await Task.Delay(400);

		float position2 = await GetPosition();

		if (position1 == position2)
			throw new ApiError("Failed to play given file.");

		await Task.Delay(1000);

		ConsoleStatus status = await GetStatus();
		Console.WriteLine(status.Title);
		if (status.Filename != filename)
			throw new ApiError($"Failed to play given file. Filename: {status.Title}");
	}

	/// <summary>
	/// Tells the music player to play the song with the specific id
	/// </summary>
	/// <param name="id">Id of the song to play</param>
	/// <returns></returns>
	public async Task PlayID(string id)
	{
		Song song = SongList.First(x => x.id == id);
		if (song?.path == null)
			throw new ApiError("Song has no path");
		await PlayFile(song.path);

		if (Cooldowns.TryGetValue(id, out Timer timer2))
		{
			timer2.Dispose();
			Cooldowns.Remove(id);
		}
		Timer timer = new Timer(6.48e+7);
		timer.Elapsed  += delegate { CooldownElapsed(id); };
		song.canBePlayed = false;
		song.cooldownExpiry = DateTime.UtcNow.AddMilliseconds(6.48e+7);
		timer.AutoReset = false;
		timer.Start();
		Cooldowns.Add(id, timer);
	}

	public Task SetPosition(float percent) => Post("setposition", new Dictionary<string, string> { ["pos"] = percent.ToString() });

	public async Task<float> SetVolume(float level)
	{
		float Clamp(float value, float min, float max)
		{
			if (value < min)
				return min;

			if (value > max)
				return max;

			return value;
		}

		float newVolume = Clamp(await GetVolume() + level, 0f, MaxVolume);
		await Post("setvolume", new Dictionary<string, string> { ["level"] = (newVolume / MaxVolume * 255f).ToString() });
		return newVolume;
	}

	public Task Stop() => Post("stop");

	~AjaxAMP()
	{
		winAmp?.Dispose();
	}
}

public class AjaxAMPConfig
{
	public string BaseUrl { get; set; }
	public float MaxVolume { get; set; } = 2.0f;
}