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
	    private FileStream logStream;
	    private StreamWriter logWriter;
	    private static FileSystemWatcher watcher;

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

	        watcher = new FileSystemWatcher(Environment.CurrentDirectory, "MusicCatConfig.json")
	        {
		        NotifyFilter = NotifyFilters.LastWrite
	        };
	        watcher.Changed += OnConfigChange;
	        watcher.EnableRaisingEvents = true;

			Listener.Start();
        }

	    private static int counter;
	    private void OnConfigChange(object sender, FileSystemEventArgs e)
	    {
		    if (counter == 0)
		    {
			    counter++;
			    return;
		    }

		    counter = 0;
		    Listener.Stop();
		    try
		    {
				logWriter?.Dispose();
				logStream?.Dispose();
			    Listener.Config = Config.ParseConfig(out logStream, out logWriter);
		    }
		    catch
		    {
			    Listener.Config = Config.DefaultConfig;
		    }
		    Listener.Start();
		    try
		    {
				MetadataStore.SongList.Clear();
			    MetadataStore.LoadMetadata();
		    }
		    catch (Exception ex)
		    {
			    if (logStream != null)
			    {
				    logWriter?.WriteLine(
					    $"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical)?.ToUpper()}: Failed to load metadata. Exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
				    logWriter?.Flush();
			    }
			    Environment.Exit(1);
		    }
	    }

		protected override void OnStop()
        {
	        Listener.Stop();
			logWriter?.Dispose();
			logStream?.Dispose();
        }
    }
}
