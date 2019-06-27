using System;
using System.IO;
using System.ServiceProcess;
using ApiListener;
using MusicCat;
using MusicCat.Metadata;

namespace ServiceWrapper
{
    public partial class MusicCatService : ServiceBase
    {
        public MusicCatService()
        {
            InitializeComponent();
        }

	    const ApiLogLevel displayLogLevel = ApiLogLevel.Debug;
	    private FileStream logStream = null;
	    private StreamWriter logWriter = null;

	    protected override void OnStart(string[] args)
        {
	        try
			{
				Listener.Config = Config.ParseConfig(out logStream, out logWriter);
			}
			catch
			{
				Listener.Config = Config.DefaultConfig;
			}

	        try
	        {
		        MetadataStore.LoadMetadata();
	        }
	        catch (Exception e)
	        {
		        if (logStream != null)
				{
					logWriter?.WriteLine(
						$"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical)?.ToUpper()}: Failed to load metadata. Exception: {e.Message}{Environment.NewLine}{e.StackTrace}");
					logWriter?.Flush();
				}
				Environment.Exit(1);
	        }

	        Listener.AttachLogger(m =>
	        {
		        if (m.Level >= displayLogLevel)
		        {
			        if (logStream != null)
			        {
				        logWriter?.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), m.Level)?.ToUpper()}: {m.Message}");
				        logWriter?.Flush();
			        }
		        }
	        });
			Listener.Start();
        }

        protected override void OnStop()
        {
	        Listener.Stop();
			logWriter?.Dispose();
			logStream?.Dispose();
        }
    }
}
