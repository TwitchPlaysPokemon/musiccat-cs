﻿using System;
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
	        string configJson = File.ReadAllText("MusicCatConfig.json");
	        try
	        {
		        Listener.Config = JsonConvert.DeserializeObject<Config>(configJson);
		        if (!string.IsNullOrWhiteSpace(Listener.Config.LogDir) &&
		            !File.Exists(Path.Combine(Listener.Config.LogDir, "MusicCatLog.txt")))
		        {
			        logStream = File.Create(Path.Combine(Listener.Config.LogDir, "MusicCatLog.txt"));
					logWriter = new StreamWriter(logStream);
		        }
				else if (!string.IsNullOrWhiteSpace(Listener.Config.LogDir))
				{
					logStream = File.OpenWrite(Path.Combine(Listener.Config.LogDir, "MusicCatLog.txt"));
					logWriter = new StreamWriter(logStream);
				}
	        }
	        catch (Exception e)
	        {
				logger(new ApiLogMessage($"Failed to de-serialize config, using default config instead. Exception: {e.Message}{Environment.NewLine}{e.StackTrace}", ApiLogLevel.Warning));
		        Listener.Config = Config.DefaultConfig;
	        }

	        try
	        {
		        Metadata.LoadMetadata(logger);
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
