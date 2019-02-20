using System;
using System.IO;
using System.ServiceProcess;
using ApiListener;
using MusicCat;
using MusicCat.Metadata;
using Newtonsoft.Json;

namespace ServiceWrapper
{
    public partial class MusicCatService : ServiceBase
    {
        public MusicCatService()
        {
            InitializeComponent();
        }

	    const ApiLogLevel displayLogLevel = ApiLogLevel.Debug;

		protected override void OnStart(string[] args)
        {
	        string configJson = File.ReadAllText("MusicCatConfig.json");
	        FileStream logStream = null;
	        StreamWriter logWriter = null;
	        try
	        {
		        Listener.Config = JsonConvert.DeserializeObject<Config>(configJson);
		        if (Listener.Config.LogDir != null &&
		            !File.Exists(Path.Combine(Listener.Config.LogDir, "MusicCatLog.txt")))
		        {
			        logStream = File.Create(Path.Combine(Listener.Config.LogDir, "MusicCatLog.txt"));
			        logWriter = new StreamWriter(logStream);
		        }
			}
	        catch
	        {
		        Listener.Config = Config.DefaultConfig;
	        }

	        try
	        {
		        Metadata.LoadMetadata();
	        }
	        catch (Exception e)
	        {
		        if (logStream != null)
				{
					logWriter?.WriteLine(
						$"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical)?.ToUpper()}: Failed to load metadata. Exception: {e.Message}{Environment.NewLine}{e.StackTrace}");
				}
				Environment.Exit(1);
	        }

	        Listener.AttachLogger(m =>
	        {
		        if (m.Level >= displayLogLevel)
		        {
			        if (logStream != null)
				        logWriter?.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), m.Level)?.ToUpper()}: {m.Message}");
		        }
	        });
			Listener.Start();
        }

        protected override void OnStop()
        {
            Listener.Stop();
        }
    }
}
