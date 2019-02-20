using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApiListener;
using YamlDotNet.Serialization;

namespace MusicCat.Metadata
{
	public class Metadata
	{
		public static List<Song> SongList = new List<Song>();

		public static void LoadMetadata(Action<ApiLogMessage> logger = null)
		{
			foreach (string directory in Directory.GetDirectories(Listener.Config.MusicBaseDir))
			{
				foreach (string filename in Directory.GetFiles(Path.Combine(Listener.Config.MusicBaseDir, directory)))
				{
					try
					{
						if (!filename.EndsWith(".yaml"))
							continue;
						logger?.Invoke(new ApiLogMessage($"Loading {Path.Combine(Path.Combine(Listener.Config.MusicBaseDir, directory), filename)}", ApiLogLevel.Debug));
						FileStream inputStream =
							new FileStream(
								Path.Combine(Path.Combine(Listener.Config.MusicBaseDir, directory), filename),
								FileMode.Open);
						object result;
						IDeserializer deserializer = new DeserializerBuilder().Build();
						using (StreamReader reader = new StreamReader(inputStream))
						{
							result = deserializer.Deserialize(reader);
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
		}

		private static List<Song> ParseMetadata(object metadata, string path)
		{
			List<Song> result = new List<Song>();
			Game obj = new Game
			{
				id = GetValue<string>(metadata, "id"),
				title = GetValue<string>(metadata, "title"),
				year = GetValue<string>(metadata, "year"),
			};
			try
			{
				obj.platform = GetValue<List<object>>(metadata, "platform").Select(x => x.ToString()).ToArray();
			}
			catch (Exception e) when (e is KeyNotFoundException || e is InvalidCastException)
			{
				obj.platform = new[] {GetValue<string>(metadata, "platform")};
			}

			try
			{
				obj.series = GetValue<string>(metadata, "series");
			}
			catch (Exception e) when (e is KeyNotFoundException || e is InvalidCastException)
			{
				obj.series = null;
			}

			try
			{
				obj.is_fanwork = GetValue<bool>(metadata, "is_fanwork");
			}
			catch (Exception e) when (e is KeyNotFoundException || e is InvalidCastException)
			{
				obj.is_fanwork = false;
			}

			foreach (object songObj in GetValue<List<object>>(metadata, "songs"))
			{
				Song song = new Song
				{
					id = GetValue<string>(songObj, "id"),
					title = GetValue<string>(songObj, "title"),
					path = Path.Combine(path, GetValue<string>(songObj, "path")),
					game = obj
				};
				try
				{
					song.types = GetValue<string[]>(songObj, "types")
						.Select(type => (SongType) Enum.Parse(typeof(SongType), type)).ToArray();
				}
				catch (Exception e) when (e is KeyNotFoundException || e is InvalidCastException)
				{
					string type = GetValue<string>(songObj, "type");
					song.types = type != null ? new[] {(SongType) Enum.Parse(typeof(SongType), type)} : null;
				}

				try
				{
					song.tags = GetValue<string[]>(songObj, "tags");
				}
				catch (Exception e) when (e is KeyNotFoundException || e is InvalidCastException)
				{
					song.tags = null;
				}

				try
				{
					song.ends = GetValue<float[]>(songObj, "ends");
				}
				catch (Exception e) when (e is KeyNotFoundException || e is InvalidCastException)
				{
					try
					{
						song.ends = GetValue<int[]>(songObj, "ends").Select(x => float.Parse(x.ToString())).ToArray();
					}
					catch (Exception e1) when (e1 is KeyNotFoundException || e1 is InvalidCastException)
					{
						try
						{
							song.ends = new[] {GetValue<float>(songObj, "ends")};
						}
						catch (Exception e2) when (e2 is KeyNotFoundException || e2 is InvalidCastException)
						{
							try
							{
								song.ends = new[] {float.Parse(GetValue<int>(songObj, "ends").ToString())};
							}
							catch (Exception e3) when (e3 is KeyNotFoundException || e3 is InvalidCastException)
							{
								try
								{
									string ends = GetValue<string>(songObj, "ends");
									string[] resultStrings = ends.Split(':');
									int resultInt = int.Parse(resultStrings[0]) * 60 + int.Parse(resultStrings[1]);
									song.ends = new[] {float.Parse(resultInt.ToString())};
								}
								catch (Exception e4) when (e4 is KeyNotFoundException || e4 is InvalidCastException)
								{
									song.ends = null;
								}
							}
						}
					}
				}

				result.Add(song);
			}
			return result;
		}

		private static T GetValue<T>(object metadata, string key)
		{
			return (T) ((Dictionary<object, object>) metadata)[key];
		}
	}
}
