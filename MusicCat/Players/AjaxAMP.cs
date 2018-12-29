using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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

        public Task Pause() => Post("pause");

        public Task Play() => Post("play");

        /// <summary>
        /// Tells the music player to play a file
        /// </summary>
        /// <param name="filename">Absolute path of file (Drive Letter must be uppercased. AjaxAMP is very very picky)</param>
        /// <returns></returns>
        public Task PlayFile(string filename) => Post("playfile", new Dictionary<string, string> { ["filename"] = filename, ["title"] = filename });

        public Task SetPosition(float percent) => Post("setposition", new Dictionary<string, string> { ["pos"] = percent.ToString() });

        public Task SetVolume(float level) => Post("setvolume", new Dictionary<string, string> { ["level"] = level.ToString() });

        public Task Stop() => Post("stop");
    }

    public class AjaxAMPConfig
    {
        public string BaseUrl { get; set; }
    }
}
