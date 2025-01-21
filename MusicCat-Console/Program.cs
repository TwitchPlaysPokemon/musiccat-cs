﻿using ApiListener;
using MusicCat;
using MusicCat.Metadata;
using static ConsoleWrapper.Logger;

namespace ConsoleWrapper;

internal static class Program
{
	private static void Main(string[] args)
	{
		var monitor = new object();
		try
		{
			Listener.Config = Config.ParseConfig(out LogStream, out LogWriter);
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
			return;
		}

		FileSystemWatcher watcher = new FileSystemWatcher(Environment.CurrentDirectory, "MusicCatConfig.json")
		{
			NotifyFilter = NotifyFilters.LastWrite
		};
		watcher.Changed += OnConfigChange;
		watcher.EnableRaisingEvents = true;

		Listener.AttachLogger(Log);
		Console.CancelKeyPress += (sender, cancelArgs) => Monitor.Pulse(monitor);
		Listener.Start();
		lock (monitor)
		{
			Monitor.Wait(monitor);
		}
		LogWriter?.Dispose();
		LogStream?.Dispose();
		Listener.Stop();
	}

	private static int counter;
	private static void OnConfigChange(object sender, FileSystemEventArgs e)
	{
		if (counter == 0)
		{
			counter++;
			return;
		}

		counter = 0;
		try
		{
			LogWriter?.Dispose();
			LogStream?.Dispose();
			Listener.Config = Config.ParseConfig(out LogStream, out LogWriter);
		}
		catch (Exception ex)
		{
			Log(new ApiLogMessage($"Failed to de-serialize config, using current config instead. Exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}", ApiLogLevel.Warning));
		}
		Listener.Stop();
		Listener.Start();
		try
		{
			MetadataStore.LoadMetadata(Log);
		}
		catch (Exception ex)
		{
			Log(new ApiLogMessage($"Failed to load metadata. Exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}", ApiLogLevel.Critical));
		}
	}
}