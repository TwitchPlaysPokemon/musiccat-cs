using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MusicCat.Metadata;
using static MusicCat.Metadata.Metadata;

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

#pragma warning disable CS1998 
		public async Task<int> Count(string category = null) => category != null ? SongList.Count(x => x.types.Contains((SongType)Enum.Parse(typeof(SongType), category))) : SongList.Count;
#pragma warning restore CS1998

		public Task Pause() => Post("pause");

        public Task Play() => Post("play");

        /// <summary>
        /// Tells the music player to play a file
        /// </summary>
        /// <param name="filename">Absolute path of file (Drive Letter must be uppercased. AjaxAMP is very very picky)</param>
        /// <returns></returns>
        public Task PlayFile(string filename) => Post("playfile", new Dictionary<string, string>
        {
	        ["filename"] = filename,
	        ["title"] = SongList.FirstOrDefault(x => x.path == filename)?.path ?? filename
        });

	    /// <summary>
	    /// Tells the music player to play the song with the specific id
	    /// </summary>
	    /// <param name="id">Id of the song to play</param>
	    /// <returns></returns>
	    public Task PlayID(string id) => Post("playid", new Dictionary<string, string>
	    {
		    ["filename"] = SongList.First(x => x.id == id).path,
		    ["title"] = SongList.First(x => x.id == id).title
	    });

        public Task SetPosition(float percent) => Post("setposition", new Dictionary<string, string> { ["pos"] = percent.ToString() });

        public Task SetVolume(float level) => Post("setvolume", new Dictionary<string, string> { ["level"] = level.ToString() });

        public Task Stop() => Post("stop");

#pragma warning disable 1998
		public async Task<List<(Song song, string game, float match)>> Search(string[] keywords,
		    string requiredTag = null,
		    float cutoff = 0.3f)
	    {
		    var results = new List<(Song song, string game, float match)>();

			Console.WriteLine(keywords.Length);
			Console.WriteLine(string.Join(", ", keywords));
		    foreach (Song song in SongList)
		    {
				if (requiredTag != null)
					if (song.tags == null || !song.tags.Contains(requiredTag))
						continue;

			    string[] haystack = song.title.ToLowerInvariant().Split(' ');
			    string[] haystack2 = MetadataList.FirstOrDefault(x => x.songs.Contains(song))?.title.ToLowerInvariant()
				    .Split(' ');

			    float ratio = 0;
			    foreach (string keyword in keywords)
			    {
				    string keyword2 = keyword.ToLowerInvariant();

				    float subratio1 = haystack.Select(word => LevenshteinRatio(keyword2, word)).Concat(new float[] {0}).Max();
				    float subratio2 = haystack2?.Select(word => LevenshteinRatio(keyword2, word))
					    .Concat(new float[] {0}).Max() ?? 0;

				    float subratio = (float)Math.Max(subratio1, subratio2 * 0.8);
				    if (subratio > 0.7)
					    ratio += subratio;
			    }

			    ratio /= keywords.Length;

			    if (ratio > cutoff)
				    results.Add((song, MetadataList.FirstOrDefault(x => x.songs.Contains(song))?.title
					    .ToLowerInvariant(), ratio));
		    }
			Console.WriteLine(results.Count);
		    return results.OrderByDescending(x => x.match).Take(5).ToList();
	    }
#pragma warning restore 1998

		//https://social.technet.microsoft.com/wiki/contents/articles/26805.c-calculating-percentage-similarity-of-2-strings.aspx
		private static float LevenshteinRatio(string source, string target)
	    {
		    if (source == null || target == null) return 0.0f;
		    if (source.Length == 0 || target.Length == 0) return 0.0f;
		    if (source == target) return 1.0f;

		    int sourceWordCount = source.Length;
		    int targetWordCount = target.Length;

		    int[,] distance = new int[sourceWordCount + 1, targetWordCount + 1];

		    // Step 2
		    for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
		    for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;

		    for (int i = 1; i <= sourceWordCount; i++)
		    {
			    for (int j = 1; j <= targetWordCount; j++)
			    {
				    // Step 3
				    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

				    // Step 4
				    distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
			    }
		    }
		    return 1.0f - (float)distance[sourceWordCount, targetWordCount] / Math.Max(source.Length, target.Length);
		}
    }

    public class AjaxAMPConfig
    {
        public string BaseUrl { get; set; }
    }
}
