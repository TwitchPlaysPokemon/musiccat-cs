using System;
using System.Threading;
using ApiListener;
using MusicCat;
using MusicCat.Metadata;
using static ConsoleWrapper.Logger;

namespace ConsoleWrapper
{
    class Program
    {
		static void Main(string[] args)
        {
            var monitor = new object();
	        try
	        {
				Listener.Config = Config.ParseConfig(out logStream, out logWriter);
	        }
	        catch (Exception e)
	        {
				Log(new ApiLogMessage($"Failed to de-serialize config, using default config instead. Exception: {e.Message}{Environment.NewLine}{e.StackTrace}", ApiLogLevel.Warning));
		        Listener.Config = Config.DefaultConfig;
	        }

	        if ((args.Length == 1 || args.Length == 2) && args[0].ToLowerInvariant().Trim() == "verify")
	        {
		        bool showUnused = args.Length == 2 && args[1].ToLowerInvariant().Trim() == "--showunused";
				bool result = MetadataStore.VerifyMetadata(showUnused, Log);
				if (result)
					Log(new ApiLogMessage("No errors.", ApiLogLevel.Info));
				return;
	        }

	        try
	        {
		        MetadataStore.LoadMetadata(Log);
	        }
	        catch (Exception e)
	        {
				Log(new ApiLogMessage($"Failed to load metadata. Exception: {e.Message}{Environment.NewLine}{e.StackTrace}", ApiLogLevel.Critical));
				Environment.Exit(1);
			}

			Listener.AttachLogger(Log);
            System.Console.CancelKeyPress += (sender, cancelArgs) => Monitor.Pulse(monitor);
            Listener.Start();
            lock (monitor)
            {
                Monitor.Wait(monitor);
            }
            Listener.Stop();
        }
    }
}
