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
		private static Random rng = new Random();

		public static List<Song> SongList = new List<Song>();
		private static Action<ApiLogMessage> logger;

		public static Task<int> Count(string category = null) => Task.Run(() => category != null ? SongList.Count(x => x.types.Contains((SongType)Enum.Parse(typeof(SongType), category))) : SongList.Count);

		public static Task<Song> GetRandomSong() => Task.Run(() => SongList[rng.Next(SongList.Count)]);

		public static Task<List<Song>> GetSongListByTag(string tag, List<Song> songList = null) => Task.Run(() =>
			songList == null
				? SongList.Where(x => x.tags != null && x.tags.Contains(tag)).ToList()
				: songList.Where(x => x.tags != null && x.tags.Contains(tag)).ToList());

		public static Task<List<Song>> GetSongListByCategory(SongType category, List<Song> songList = null) => Task.Run(
			() =>
				songList == null
					? SongList.Where(x => x.types != null && x.types.Length != 0 && x.types.Contains(category)).ToList()
					: songList.Where(x => x.types != null && x.types.Length != 0 && x.types.Contains(category))
						.ToList());

		public static Task<List<Song>> GetSongListByGame(string game, List<Song> songList = null) => Task.Run(() =>
			songList == null
				? SongList.Where(x => x.game.id == game).ToList()
				: songList.Where(x => x.game.id == game).ToList());
		
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
							$"Loading {Path.Combine(Listener.Config.MusicBaseDir, directory, filename)}",
							ApiLogLevel.Debug));
						FileStream inputStream =
							new FileStream(
								Path.Combine(Listener.Config.MusicBaseDir, directory, filename),
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
							Song duplicate = SongList.FirstOrDefault(x => x.id == song.id);
							if (duplicate != null)
								throw new DuplicateSongException(
									$"ID {song.id} has been declared twice, in songs {song.title} and {duplicate.title}");
							if (File.Exists(song.path))
							{
								SongList.AddRange(new List<Song> { song });
								continue;
							}
							logger?.Invoke(new ApiLogMessage(
								$"Song file at {song.path} does not exist, the song will not play",
								ApiLogLevel.Warning));
							song.path = null;
							SongList.AddRange(new List<Song> { song });
						}
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

		public static bool VerifyMetadata(bool showUnused, Action<ApiLogMessage> logger = null)
		{
			bool ret = true;
			IDeserializer deserializer = new DeserializerBuilder().Build();
			List<string> files = new List<string>();
			List<string> processed = new List<string>();
			foreach (string directory in Directory.EnumerateDirectories(Listener.Config.MusicBaseDir, "*",
				SearchOption.AllDirectories))
			{
				if (Listener.Config.SongFileDir != null)
				{
					if (!directory.Contains(".git") && directory.ToLowerInvariant() != "other" &&
					    !directory.ToLowerInvariant().Contains("other\\") &&
					    !directory.ToLowerInvariant().Contains("other/"))
					{
						files.AddRange(Directory
							.EnumerateFiles(Path.Combine(Listener.Config.SongFileDir, directory), "*")
							.Where(filename => !filename.EndsWith(".pos") && !filename.EndsWith("sflib") && !filename.EndsWith("sf2lib")
							                   && !filename.EndsWith(".sth")) //exclude song libraries
							.Select(filename => Path.Combine(Listener.Config.SongFileDir, directory, filename)));
					}
				}
				else if (!directory.Contains(".git") && directory.ToLowerInvariant() != "other" &&
				         !directory.ToLowerInvariant().Contains("other\\") &&
				         !directory.ToLowerInvariant().Contains("other/"))
					files.AddRange(Directory
						.EnumerateFiles(Path.Combine(Listener.Config.MusicBaseDir, directory), "*")
						.Where(filename => !filename.EndsWith(".yaml") && !filename.EndsWith(".pos") && !filename.EndsWith("sflib")
						                   && !filename.EndsWith("sf2lib") && !filename.EndsWith(".sth"))
						.Select(filename => Path.Combine(Listener.Config.MusicBaseDir, directory, filename)));

				foreach (string filename in Directory.EnumerateFiles(
					Path.Combine(Listener.Config.MusicBaseDir, directory), "*.yaml"))
				{
					try
					{
						FileStream inputStream =
							new FileStream(Path.Combine(Listener.Config.MusicBaseDir, directory, filename),
								FileMode.Open);
						Metadata result;
						using (StreamReader reader = new StreamReader(inputStream))
						{
							result = deserializer.Deserialize<Metadata>(reader);
						}

						string path = Path.Combine(Listener.Config.SongFileDir ?? Listener.Config.MusicBaseDir,
							directory);
						string current = Path.Combine(Listener.Config.MusicBaseDir, directory, filename);

						List<Song> songs = ParseMetadata(result, path);

						Game game = songs.First().game;

						if (game.id == null)
						{
							logger?.Invoke(new ApiLogMessage($"Missing Game ID in file {current}",
								ApiLogLevel.Warning));
							ret = false;
						}

						if (game.title == null)
						{
							logger?.Invoke(new ApiLogMessage($"Missing Game Title in file {current}",
								ApiLogLevel.Warning));
							ret = false;
						}

						if (game.platform == null || game.platform.Length == 0)
						{
							logger?.Invoke(new ApiLogMessage($"Missing Game Platform in file {current}",
								ApiLogLevel.Warning));
							ret = false;
						}

						if (game.year == null)
						{
							logger?.Invoke(new ApiLogMessage($"Missing Game Year in file {current}",
								ApiLogLevel.Warning));
							ret = false;
						}

						foreach (Song song in songs)
						{
							if (song.id == null)
							{
								logger?.Invoke(new ApiLogMessage($"Missing Song ID in file {current}",
									ApiLogLevel.Warning));
								ret = false;
							}

							if (song.title == null)
							{
								logger?.Invoke(new ApiLogMessage($"Missing Song Title in file {current}",
									ApiLogLevel.Warning));
								ret = false;
							}

							if (song.types == null || song.types.Length == 0)
							{
								logger?.Invoke(new ApiLogMessage($"Missing Song Type in file {current}",
									ApiLogLevel.Warning));
								ret = false;
							}

							if (song.path != null && processed.Contains(song.path))
							{
								logger?.Invoke(new ApiLogMessage(
									$"Duplicate reference to song file in file {current}. Path: {song.path}",
									ApiLogLevel.Warning));
								ret = false;
							}

							if (song.path != null && !files.Contains(song.path))
							{
								logger?.Invoke(new ApiLogMessage(
									$"Song file referenced in file {current} is missing. Path: {song.path}",
									ApiLogLevel.Warning));
								ret = false;
							}

							if (song.path == null)
							{
								logger?.Invoke(new ApiLogMessage(
									$"Missing Song Path in file {current}",
									ApiLogLevel.Warning));
								ret = false;
							}

							if (song.path != null && !processed.Contains(song.path) && files.Contains(song.path))
								processed.Add(song.path);
							if (song.id == null) continue;
							Song duplicate = SongList.FirstOrDefault(x => x.id == song.id);
							if (duplicate != null)
							{
								logger?.Invoke(new ApiLogMessage(
									$"ID {song.id} has been declared twice, in songs {song.title} and {duplicate.title}",
									ApiLogLevel.Warning));
								ret = false;
							}

							SongList.Add(song);
						}
					}
					catch (Exception e)
					{
						logger?.Invoke(new ApiLogMessage(
							$"Exception encountered in file {Path.Combine(Listener.Config.MusicBaseDir, directory, filename)}:" +
							$"{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}{Environment.NewLine}Skipping file.",
							ApiLogLevel.Critical));
					}
				}
			}

			if (!showUnused) return ret;

			IEnumerable<string> unused = files.Where(x => !processed.Contains(x));
			foreach (string path in unused)
			{
				ret = false;
				logger?.Invoke(new ApiLogMessage($"Song file at {path} is unused.", ApiLogLevel.Warning));
			}

			return ret;
		}

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
			SongList.Clear();
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
