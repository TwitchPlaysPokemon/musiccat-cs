using System;
using System.IO;
using System.Threading;
using ApiListener;
using MusicCat;
using MusicCat.Metadata;

namespace ConsoleWrapper
{
    class Program
    {
        const ApiLogLevel displayLogLevel = ApiLogLevel.Debug;
		private static FileStream logStream = null;
		private static StreamWriter logWriter = null;

		private static Action<ApiLogMessage> logger = m =>
		{
			if (m.Level >= displayLogLevel)
			{
				switch (m.Level)
				{
					case ApiLogLevel.Error:
					case ApiLogLevel.Critical:
						Console.ForegroundColor = ConsoleColor.Red;
						break;
					case ApiLogLevel.Warning:
						Console.ForegroundColor = ConsoleColor.Yellow;
						break;
					case ApiLogLevel.Debug:
					case ApiLogLevel.Info:
						Console.ForegroundColor = ConsoleColor.White;
						break;
				}

				Console.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), m.Level).ToUpper()}: {m.Message}");
				if (logStream != null)
					logWriter?.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), m.Level)?.ToUpper()}: {m.Message}");

				Console.ForegroundColor = ConsoleColor.White;
			}
		};

		static void Main(string[] args)
        {
            var monitor = new object();
	        try
	        {
				Listener.Config = Config.ParseConfig(out logStream, out logWriter);
	        }
	        catch (Exception e)
	        {
				logger(new ApiLogMessage($"Failed to de-serialize config, using default config instead. Exception: {e.Message}{Environment.NewLine}{e.StackTrace}", ApiLogLevel.Warning));
		        Listener.Config = Config.DefaultConfig;
	        }

	        try
	        {
		        MetadataStore.LoadMetadata(logger);
	        }
	        catch (Exception e)
	        {
				logger(new ApiLogMessage($"Failed to load metadata. Exception: {e.Message}{Environment.NewLine}{e.StackTrace}", ApiLogLevel.Critical));
				Environment.Exit(1);
			}

			Listener.AttachLogger(logger);
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
