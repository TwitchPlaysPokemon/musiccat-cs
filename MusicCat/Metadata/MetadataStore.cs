using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using ApiListener;
using YamlDotNet.Serialization;

namespace MusicCat.Metadata
{
	public class MetadataStore
	{
		private static FileSystemWatcher watcher;
		private static Timer timer;

		public static List<Song> SongList = new List<Song>();
		private static Action<ApiLogMessage> logger;

		public static Task<int> Count(string category = null) => Task.Run(() => category != null ? SongList.Count(x => x.types.Contains((SongType)Enum.Parse(typeof(SongType), category))) : SongList.Count);

		public static async void LoadMetadata(Action<ApiLogMessage> logger = null) => await Task.Run(() =>
		{
			if (watcher == null)
			{
				MetadataStore.logger = logger;
				watcher = new FileSystemWatcher(Listener.Config.MusicBaseDir)
				{
					NotifyFilter = NotifyFilters.Size,
					IncludeSubdirectories = true
				};
				watcher.Changed += OnFileChanged;
				watcher.EnableRaisingEvents = true;
			}
			IDeserializer deserializer = new DeserializerBuilder().Build();
			foreach (string directory in Directory.EnumerateDirectories(Listener.Config.MusicBaseDir, "*", SearchOption.AllDirectories))
			{
				foreach (string filename in Directory.EnumerateFiles(Path.Combine(Listener.Config.MusicBaseDir, directory), "*.yaml"))
				{
					try
					{
						logger?.Invoke(new ApiLogMessage(
							$"Loading {Path.Combine(Path.Combine(Listener.Config.MusicBaseDir, directory), filename)}",
							ApiLogLevel.Debug));
						FileStream inputStream =
							new FileStream(
								Path.Combine(Path.Combine(Listener.Config.MusicBaseDir, directory), filename),
								FileMode.Open);
						Metadata result;
						using (StreamReader reader = new StreamReader(inputStream))
						{
							result = deserializer.Deserialize<Metadata>(reader);
						}

						string path = Path.Combine(Listener.Config.SongFileDir ?? Listener.Config.MusicBaseDir, directory);

						List<Song> songs = ParseMetadata(result, path);

						foreach (Song song in songs)
						{
							if (File.Exists(song.path)) continue;
							logger?.Invoke(new ApiLogMessage(
								$"Song file at {song.path} does not exist, the song will not play",
								ApiLogLevel.Warning));
							song.path = null;
							Song duplicate = SongList.FirstOrDefault(x => x.id == song.id);
							if (duplicate != null)
								throw new DuplicateSongException(
									$"ID {song.id} has been declared twice, in songs {song.title} and {duplicate.title}");
						}

						SongList.AddRange(songs);
					}
					catch (DuplicateSongException e)
					{
						logger?.Invoke(new ApiLogMessage($"Exception processing metadata file {Path.Combine(Path.Combine(Listener.Config.MusicBaseDir, directory), filename)}: {e.Message}{Environment.NewLine}{e.StackTrace}" +
														$"{Environment.NewLine}Cannot continue", ApiLogLevel.Critical));
						break;
					}
					catch (Exception e)
					{
						logger?.Invoke(new ApiLogMessage($"Exception processing metadata file {Path.Combine(Path.Combine(Listener.Config.MusicBaseDir, directory), filename)}: {e.Message}{Environment.NewLine}{e.StackTrace}" +
														$"{Environment.NewLine}Attempting to continue", ApiLogLevel.Critical));
					}
				}
			}
		});

		private static void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			if (timer == null)
			{
				timer = new Timer(600000);
				timer.Elapsed += OnTimerElapsed;
				timer.AutoReset = false;
				timer.Start();
			}
			else
			{
				lock (timer)
				{
					timer.Stop();
					timer.Start();
				}
			}
		}

		private static void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			LoadMetadata(logger);
			timer.Dispose();
			timer = null;
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
				watcher.Changed -= OnFileChanged;
				watcher.Dispose();
			}

			timer?.Dispose();
		}
	}
}
