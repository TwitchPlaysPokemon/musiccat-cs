namespace MusicCat
{
    public class Config
    {
        public int HttpPort { get; set; }
        public string MusicBaseDir { get; set; }
		public string LogDir { get; set; }
        public Players.AjaxAMPConfig AjaxAMPConfig { get; set; }

        public static Config DefaultConfig => new Config
        {
            HttpPort = 7337,
            MusicBaseDir = "D:\\Music",
			LogDir = "D:\\Music",
            AjaxAMPConfig = new Players.AjaxAMPConfig
            {
                BaseUrl = "http://localhost:5151"
            }
        };
    }
}
