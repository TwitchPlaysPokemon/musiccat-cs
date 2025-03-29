#nullable disable

using System.Diagnostics;
using System.Globalization;
using System.Xml.Serialization;
using MusicCat.Model;

namespace MusicCat.Players;

public class AjaxAMP(AjaxAMPConfig config, string winampPath, string songFilePath, MusicLibrary musicLibrary) : IPlayer
{
	private readonly HttpClient _httpClient = new()
	{
		BaseAddress = new Uri(config.BaseUrl)
	};
	private Process _winAmp;
	private readonly float _maxVolume = config.MaxVolume;

	private Task<string> SendCommand(string command, Dictionary<string, string> args = null, bool post = false)
	{
		var argsDigest = string.Join("&", (args ?? new Dictionary<string, string>()).Keys.Select(k => $"{Uri.EscapeDataString(k)}={Uri.EscapeDataString(args[k])}"));
		var request = new HttpRequestMessage(post ? HttpMethod.Post : HttpMethod.Get, command + (post || args == null ? "" : "?" + argsDigest));
		if (post)
		{
			request.Content = new StringContent(argsDigest);
		}
		return _httpClient.SendAsync(request).Result.Content.ReadAsStringAsync();
	}

	private Task<string> Post(string command, Dictionary<string, string> args = null) => SendCommand(command, args, true);

	private Task<string> Get(string command, Dictionary<string, string> args = null) => SendCommand(command, args);

	public async Task<float> GetPosition() => float.Parse(await Get("getposition"));

	public async Task<float> GetVolume() => float.Parse(await Get("getvolume")) / 255f * _maxVolume;

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

		if (string.IsNullOrEmpty(winampPath))
			throw new Exception("Winamp path in the config is not set."); // TODO specific exception?

		if (!File.Exists(winampPath))
			throw new Exception("There is no file at the given path.");

		_winAmp = new Process { StartInfo = { FileName = winampPath } };
		_winAmp.Start();

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
		var fullPath = Path.GetFullPath(Path.Join(songFilePath, filename));
		await Post("playfile", new Dictionary<string, string>
		{
			["filename"] = fullPath,
			["title"] = filename
		});

		float position1 = await GetPosition();

		await Task.Delay(400);

		float position2 = await GetPosition();

		if (position1 == position2)
			throw new Exception("Failed to play given file.");

		await Task.Delay(1000);

		ConsoleStatus status = await GetStatus();
		Console.WriteLine(status.Title);
		if (status.Filename != filename)
			throw new Exception($"Failed to play given file. Filename: {status.Title}");
	}

	/// <summary>
	/// Tells the music player to play the song with the specific id
	/// </summary>
	/// <param name="id">Id of the song to play</param>
	/// <returns></returns>
	public async Task PlayID(string id)
	{
		Song song = await musicLibrary.Get(id)
		            ?? throw new Exception($"Could not find song with id: {id}");
		await PlayFile(song.Path);
	}

	public Task SetPosition(float percent) => Post("setposition", new Dictionary<string, string> { ["pos"] = percent.ToString() });

	public async Task SetVolume(float level)
	{
		await Post("setvolume", new Dictionary<string, string> { ["level"] = (level / _maxVolume * 255f).ToString() });
	}

	public Task Stop() => Post("stop");

	~AjaxAMP()
	{
		_winAmp?.Dispose();
	}
}

public class AjaxAMPConfig
{
	public string BaseUrl { get; init; } = "http://localhost:5151/";
	public float MaxVolume { get; init; } = 2.0f;
}