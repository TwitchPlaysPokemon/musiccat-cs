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
