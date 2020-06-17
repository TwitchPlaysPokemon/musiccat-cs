using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiListener;
using static MusicCat.Metadata.MetadataStore;

namespace MusicCat.Metadata
{
	public static class MetadataCommands
	{
		private static Random rng = new Random();

		public static Task<int> Count(string category = null) => MetadataStore.Count(category);

		public static Task<Song> GetRandomSong() => MetadataStore.GetRandomSong();

		public static async Task<Song> GetRandomSongBy(string[] args)
		{
			List<Song> filterList = null;
			string[] processedArgs = int.TryParse(args.Last(), out _) ? args.Take(args.Length - 1).ToArray() : args;

			if (args.Length == 0)
				throw new ApiError("Arguments cannot be null.");

			for (int i = 0; i < processedArgs.Length; i += 2)
			{
				string filterType = args[i].Trim().ToLowerInvariant();
				switch (filterType)
				{
					case "tag":
						filterList = await GetSongListByTag(args[i + 1].Trim().ToLowerInvariant(), filterList);
						break;
					case "category":
						filterList = await GetSongListByCategory((SongType)Enum.Parse(typeof(SongType),
							args[i + 1].Trim().ToLowerInvariant()), filterList);
						break;
					case "game":
						filterList = await GetSongListByGame(args[i + 1].Trim().ToLowerInvariant(), filterList);
						break;
					default:
						throw new ApiError($"Unrecognised filter: {filterType}");
				}
			}

			if (int.TryParse(args.Last(), out int result))
				filterList = (filterList ?? SongList).Where(x => x.ends != null && x.ends >= result).ToList();

			return filterList == null || filterList.Count == 0 ? null : filterList[rng.Next(filterList.Count)];
		}

		public static Task<List<(Song song, float match)>> Search(string[] keywords,
			string requiredTag = null,
			float cutoff = 0.3f) => Task.Run(() =>
		{
			if (keywords.Any(x => x.StartsWith("required_tag=")))
			{
				requiredTag = keywords.First(x => x.StartsWith("required_tag=")).Replace("required_tag=", "");
				keywords = keywords.Where(x => !x.StartsWith("required_tag=")).ToArray();
			}
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
					float subratio2 = haystack2.Select(word => keyword2.LevenshteinRatio(word))
						.Concat(new float[] { 0 }).Max();

					float subratio = Math.Max(subratio1, subratio2 * 0.9f);
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
}
