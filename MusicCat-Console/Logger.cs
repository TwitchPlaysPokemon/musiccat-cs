using System;
using System.IO;
using ApiListener;

namespace ConsoleWrapper;

public static class Logger
{
	const ApiLogLevel displayLogLevel = ApiLogLevel.Debug;
	public static FileStream logStream = null;
	public static StreamWriter logWriter = null;
	public static void Log(ApiLogMessage message)
	{
		if (message.Level >= displayLogLevel)
		{
			switch (message.Level)
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

			Console.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), message.Level).ToUpper()}: {message.Message}");
			if (logStream != null)
			{
				logWriter?.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), message.Level).ToUpper()}: {message.Message}");
				logWriter?.Flush();
			}

			Console.ForegroundColor = ConsoleColor.White;
		}
	}
}