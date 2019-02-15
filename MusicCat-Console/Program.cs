using System;
using System.IO;
using System.Threading;
using ApiListener;
using MusicCat;
using MusicCat.Metadata;
using Newtonsoft.Json;

namespace ConsoleWrapper
{
    class Program
    {
        const ApiLogLevel displayLogLevel = ApiLogLevel.Debug;

        static void Main(string[] args)
        {
	        FileStream logStream = null;
	        StreamWriter logWriter = null;
            var monitor = new object();
	        string configJson = File.ReadAllText("MusicCatConfig.json");
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
				Console.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Warning).ToUpper()}: Failed to de-serialize config, using default config instead.");
		        Listener.Config = Config.DefaultConfig;
	        }

	        try
	        {
		        Metadata.LoadMetadata();
	        }
	        catch (Exception e)
	        {
		        if (logStream != null && logWriter != null)
		        {
			        logWriter.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical).ToUpper()}: Failed to load metadata. Stop.");
			        logWriter.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical).ToUpper()}: {e.Message}{Environment.NewLine}{e.StackTrace}");
			        logWriter.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical).ToUpper()}: Inner exception: {e.InnerException.Message ?? "none"}{Environment.NewLine}{e.InnerException.StackTrace ?? "none"}");
				}
		        Console.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical).ToUpper()}: Failed to load metadata. Stop.");
				Console.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical).ToUpper()}: {e.Message}{Environment.NewLine}{e.StackTrace}");
		        Console.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical).ToUpper()}: Inner exception: {e.InnerException.Message ?? "none"}{Environment.NewLine}{e.InnerException.StackTrace ?? "none"}");
				Environment.Exit(1);
			}
			Listener.AttachLogger(m =>
            {
	            if (m.Level >= displayLogLevel)
	            {
		            Console.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), m.Level).ToUpper()}: {m.Message}");
					if (logStream != null)
						logWriter?.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), m.Level)?.ToUpper()}: {m.Message}");
	            }
            });
            Console.CancelKeyPress += (sender, cancelArgs) => Monitor.Pulse(monitor);
            Listener.Start();
            lock (monitor)
            {
                Monitor.Wait(monitor);
            }
            Listener.Stop();
        }
    }
}
