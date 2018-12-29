using System;
using System.Threading;

namespace ConsoleWrapper
{
    class Program
    {
        static void Main(string[] args)
        {
            var monitor = new object();
            //TODO: Load config
            MusicCat.Listener.Config = MusicCat.Config.DefaultConfig;
            Console.CancelKeyPress += new ConsoleCancelEventHandler((sender, cancelArgs) => Monitor.Pulse(monitor));
            MusicCat.Listener.Start();
            Console.WriteLine($"Listening for HTTP connections on http://localhost:{MusicCat.Listener.Config.HttpPort}");
            lock (monitor)
            {
                Monitor.Wait(monitor);
            }
            Console.WriteLine("Stopping.");
            MusicCat.Listener.Stop();
        }
    }
}
