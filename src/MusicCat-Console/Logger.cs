#nullable disable

using ApiListener;

namespace ConsoleWrapper;

public static class Logger
{
	const ApiLogLevel DisplayLogLevel = ApiLogLevel.Debug;
	public static FileStream LogStream = null;
	public static StreamWriter LogWriter = null;
	public static void Log(ApiLogMessage message)
	{
		if (message.Level >= DisplayLogLevel)
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
			if (LogStream != null)
			{
				LogWriter?.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), message.Level).ToUpper()}: {message.Message}");
				LogWriter?.Flush();
			}

			Console.ForegroundColor = ConsoleColor.White;
		}
	}
}