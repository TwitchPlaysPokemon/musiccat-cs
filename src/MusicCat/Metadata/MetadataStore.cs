#nullable disable

using System.Timers;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace MusicCat.Metadata;

public class MetadataStore
{
	private static FileSystemWatcher _watcher;
	private static Timer _timer;
	private static readonly Random Rng = new();

	public static List<Song> SongList = [];
	public static readonly Dictionary<string, Timer> Cooldowns = new();
	private static ILogger _logger;

	public static Task<int> Count(string category = null) => Task.Run(() => category != null ? SongList.Count(x => x.Types.Contains((SongType)Enum.Parse(typeof(SongType), category))) : SongList.Count);

	public static Task<Song> GetRandomSong() => Task.Run(() => SongList[Rng.Next(SongList.Count)]);

	public static Task<List<Song>> GetSongListByTag(string tag, List<Song> songList = null) => Task.Run(() =>
		(songList ?? SongList).Where(x => x.Tags != null && x.Tags.Contains(tag) && x.canBePlayed).ToList());

	public static Task<List<Song>> GetSongListByCategory(SongType category, List<Song> songList = null) => Task.Run(
		() =>
			(songList ?? SongList).Where(x =>
				x.Types.Count != 0 && x.Types.Contains(category) && x.canBePlayed).ToList());

	public static Task<List<Song>> GetSongListByGame(string game, List<Song> songList = null) => Task.Run(() =>
		(songList ?? SongList).Where(x => x.Game.Id == game && x.canBePlayed).ToList());

	public static async void LoadMetadata(ILogger logger) => await Task.Run(() =>
	{
		if (_watcher == null)
		{
			_logger = logger;
			_watcher = new FileSystemWatcher(Listener.Config.MusicBaseDir)
			{
				NotifyFilter = NotifyFilters.Size,
				IncludeSubdirectories = true
			};
			_watcher.Changed += OnFileChanged;
			_watcher.EnableRaisingEvents = true;
		}

		MetadataLoadResult result = MusicLibrary.ReadMetadata(Listener.Config.MusicBaseDir, Listener.Config.SongFileDir).Result;
		foreach (string warning in result.Warnings) 
			logger.LogWarning(warning);

		SongList = result.Songs.Values.ToList();
	});

	public static bool VerifyMetadata(bool showUnused, ILogger logger)
	{
		MetadataLoadResult result = MusicLibrary.ReadMetadata(Listener.Config.MusicBaseDir, Listener.Config.SongFileDir).Result;

		foreach (string warning in result.Warnings) 
			logger.LogWarning(warning);
		
		if (!showUnused) return result.Warnings.Count == 0;

		return result.Warnings.Count == 0; // TODO unused song files
	}

	private static void OnFileChanged(object sender, FileSystemEventArgs e)
	{
		if (_timer == null)
		{
			_timer = new Timer(600000);
			_timer.Elapsed += OnTimerElapsed;
			_timer.AutoReset = false;
			_timer.Start();
		}
		else
		{
			lock (_timer)
			{
				_timer.Stop();
				_timer.Start();
			}
		}
	}

	private static void OnTimerElapsed(object sender, ElapsedEventArgs e)
	{
		SongList.Clear();
		LoadMetadata(_logger);
		_timer.Dispose();
		_timer = null;
	}

	public static void CooldownElapsed(string id)
	{
		Timer timer = Cooldowns[id];
		timer.Dispose();
		Song song = SongList.FirstOrDefault(x => x.Id == id);
		Cooldowns.Remove(id);
		if (song == null) return;
		// TODO re-implement cooldown handling
		// song.canBePlayed = true;
		// song.cooldownExpiry = null;
	}

	~MetadataStore()
	{
		if (_watcher != null)
		{
			_watcher.Changed -= OnFileChanged;
			_watcher.Dispose();
		}

		foreach (KeyValuePair<string, Timer> kvp in Cooldowns)
		{
			kvp.Value.Dispose();
		}

		_timer?.Dispose();
	}
}