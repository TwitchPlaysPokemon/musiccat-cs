using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ApiListener;
using MusicCat.Metadata;
using static MusicCat.Metadata.MetadataStore;

namespace MusicCat.Players
{
	public class AjaxAMP : IPlayer
	{
		private HttpClient httpClient;

		public AjaxAMP(AjaxAMPConfig config)
		{
			httpClient = new HttpClient
			{
				BaseAddress = new Uri(config.BaseUrl)
			};
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

		public async Task<float> GetVolume() => float.Parse(await Get("getvolume"));

		private async Task<ConsoleStatus> GetStatus()
		{
			ConsoleStatus status;
			XmlSerializer serializer = new XmlSerializer(typeof(ConsoleStatus));
			string serialized = await Get("consolestatus.xml");
			using (StringReader reader = new StringReader(serialized))
			{
				status = (ConsoleStatus) serializer.Deserialize(reader);
			}
			return status;
		}

		public Task<int> Count(string category = null) => MetadataStore.Count(category);

		public async Task Launch()
		{
			if (Process.GetProcessesByName("Winamp").Length > 0)
				return;

			if (string.IsNullOrEmpty(Listener.Config.WinampPath))
				throw new ApiError("Winamp path in the config is not set.");

			if (!File.Exists(Listener.Config.WinampPath))
				throw new ApiError("There is no file at the given path.");

			Process process = new Process { StartInfo = { FileName = Listener.Config.WinampPath } };
			process.Start();

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

			await Task.Delay(200);

			float position2 = await GetPosition();

			if (position1 == position2)
				throw new ApiError("Failed to play given file.");

			ConsoleStatus status = await GetStatus();
			if (status.filename != filename)
				throw new ApiError("Failed to play given file.");
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
		}

		public Task SetPosition(float percent) => Post("setposition", new Dictionary<string, string> { ["pos"] = percent.ToString() });

		public Task SetVolume(float level) => Post("setvolume", new Dictionary<string, string> { ["level"] = level.ToString() });

		public Task Stop() => Post("stop");

		public Task<List<(Song song, float match)>> Search(string[] keywords,
			string requiredTag = null,
			float cutoff = 0.3f) => Task.Run(() =>
			{
				var results = new List<(Song song, float match)>();

				foreach (Song song in SongList)
				{
					if (requiredTag != null)
						if (song.tags == null || !song.tags.Contains(requiredTag))
							continue;

					if (song.path == null) continue;

					string[] haystack = song.title.ToLowerInvariant().Split(' ');
					string[] haystack2 = song.game.title.ToLowerInvariant().Split(' ');

					float ratio = 0;
					foreach (string keyword in keywords)
					{
						string keyword2 = keyword.ToLowerInvariant();

						float subratio1 = haystack.Select(word => keyword2.LevenshteinRatio(word)).Concat(new float[] { 0 }).Max();
						float subratio2 = haystack2?.Select(word => keyword2.LevenshteinRatio(word))
							.Concat(new float[] { 0 }).Max() ?? 0;

						float subratio = (float)Math.Max(subratio1, subratio2);
						if (subratio > 0.7)
							ratio += subratio;
					}

					ratio /= keywords.Length;

					if (ratio > cutoff)
						results.Add((song, ratio));
				}
				return results.OrderByDescending(x => x.match).Take(5).ToList();
			});
	}

	public class AjaxAMPConfig
	{
		public string BaseUrl { get; set; }
	}
}
