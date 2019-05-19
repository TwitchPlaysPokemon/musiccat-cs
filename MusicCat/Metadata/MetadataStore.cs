using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApiListener;
using YamlDotNet.Serialization;

namespace MusicCat.Metadata
{
	public class MetadataStore
	{
		private static FileSystemWatcher watcher;

		public static List<Song> SongList = new List<Song>();
		private static Action<ApiLogMessage> logger;

		public static Task<int> Count(string category = null) => Task.Run(() => category != null ? SongList.Count(x => x.types.Contains((SongType)Enum.Parse(typeof(SongType), category))) : SongList.Count);

		public static async void LoadMetadata(Action<ApiLogMessage> logger = null) => await Task.Run(() =>
		{
			if (watcher == null)
			{
				watcher = new FileSystemWatcher(Listener.Config.MusicBaseDir);
				watcher.NotifyFilter = NotifyFilters.LastWrite;
				watcher.Changed += OnChanged;
			}
			IDeserializer deserializer = new DeserializerBuilder().Build();
			foreach (string directory in Directory.EnumerateDirectories(Listener.Config.MusicBaseDir))
			{
				foreach (string filename in Directory.EnumerateFiles(Path.Combine(Listener.Config.MusicBaseDir, directory), "*.yaml", SearchOption.AllDirectories))
				{
					try
					{
						logger?.Invoke(new ApiLogMessage($"Loading {Path.Combine(Path.Combine(Listener.Config.MusicBaseDir, directory), filename)}", ApiLogLevel.Debug));
						FileStream inputStream =
							new FileStream(
								Path.Combine(Path.Combine(Listener.Config.MusicBaseDir, directory), filename),
								FileMode.Open);
						Metadata result;
						using (StreamReader reader = new StreamReader(inputStream))
						{
							result = deserializer.Deserialize<Metadata>(reader);
						}
						List<Song> songs = ParseMetadata(result, Path.Combine(Listener.Config.MusicBaseDir, directory));

						foreach (Song song in songs)
						{
							if (File.Exists(song.path)) continue;
							logger?.Invoke(new ApiLogMessage(
								$"Song file at {song.path} does not exist, the song will not play",
								ApiLogLevel.Warning));
							song.path = null;
						}

						SongList.AddRange(songs);
					}
					catch (Exception e)
					{
						logger?.Invoke(new ApiLogMessage($"Exception processing metadata file {Path.Combine(Path.Combine(Listener.Config.MusicBaseDir, directory), filename)}: {e.Message}{Environment.NewLine}{e.StackTrace}" +
														$"{Environment.NewLine}Attempting to continue", ApiLogLevel.Critical));
					}
				}
			}
		});

		private static void OnChanged(object sender, FileSystemEventArgs e)
		{
			bool flag = false;
			try
			{
				using (FileStream inputStream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.None))
					if (inputStream.Length > 0)
						flag = true;
			}
			catch (Exception)
			{
				return;
			}
			if (flag)
				LoadMetadata(logger);
		}

		private static List<Song> ParseMetadata(Metadata metadata, string path)
		{
			List<Song> result = new List<Song>();
			foreach (Song song in metadata.songs)
			{
				song.game = new Game
				{
					id = metadata.id,
					title = metadata.title,
					series = metadata.series,
					year = metadata.year,
					platform = metadata.platform,
					is_fanwork = metadata.is_fanwork
				};
				song.path = Path.Combine(path, song.path);
				result.Add(song);
			}
			return result;
		}

		~MetadataStore()
		{
			if (watcher != null)
			{
				watcher.Changed -= OnChanged;
				watcher.Dispose();
			}
		}
	}
}
