using System;
using System.Threading;

namespace ConsoleWrapper
{
    class Program
    {
        const ApiListener.ApiLogLevel displayLogLevel = ApiListener.ApiLogLevel.Debug;

        static void Main(string[] args)
        {
            var monitor = new object();
            //TODO: Load config
            MusicCat.Listener.Config = MusicCat.Config.DefaultConfig;
            MusicCat.Listener.AttachLogger(m =>
            {
                if (m.Level >= displayLogLevel)
                    Console.WriteLine($"{Enum.GetName(typeof(ApiListener.ApiLogLevel), m.Level).ToUpper()}: {m.Message}");
            });
            Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, cancelArgs) => Monitor.Pulse(monitor));
            MusicCat.Listener.Start();
            lock (monitor)
            {
                Monitor.Wait(monitor);
            }
            MusicCat.Listener.Stop();
        }
    }
}
