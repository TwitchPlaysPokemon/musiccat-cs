using ApiListener;
using MusicCat.Metadata;

namespace MusicCat.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    private const ApiLogLevel DisplayLogLevel = ApiLogLevel.Debug;

    private FileStream? _logStream;
    private StreamWriter? _logWriter;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Listener.Config = Config.ParseConfig(out _logStream, out _logWriter);
        }
        catch
        {
            Listener.Config = Config.DefaultConfig;
        }

        try
        {
            MetadataStore.LoadMetadata();
        }
        catch (Exception e)
        {
            if (_logStream != null)
            {
                _logWriter?.WriteLine(
                    $"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical)?.ToUpper()}: Failed to load metadata. Exception: {e.Message}{Environment.NewLine}{e.StackTrace}");
                _logWriter?.Flush();
            }
            Environment.Exit(1);
        }

        Listener.AttachLogger(m =>
        {
            if (m.Level >= DisplayLogLevel)
            {
                if (_logStream != null)
                {
                    _logWriter?.WriteLine($"{Enum.GetName(typeof(ApiLogLevel), m.Level)?.ToUpper()}: {m.Message}");
                    _logWriter?.Flush();
                }
            }
        });

        var watcher = new FileSystemWatcher(Environment.CurrentDirectory, "MusicCatConfig.json")
        {
            NotifyFilter = NotifyFilters.LastWrite
        };
        watcher.Changed += OnConfigChange;
        watcher.EnableRaisingEvents = true;

        Listener.Start();
        await Task.CompletedTask;
    }
    
    private static int counter;
    private void OnConfigChange(object sender, FileSystemEventArgs e)
    {
        if (counter == 0)
        {
            counter++;
            return;
        }

        counter = 0;
        Listener.Stop();
        try
        {
            _logWriter?.Dispose();
            _logStream?.Dispose();
            Listener.Config = Config.ParseConfig(out _logStream, out _logWriter);
        }
        catch
        { }
        Listener.Start();
        try
        {
            MetadataStore.LoadMetadata();
        }
        catch (Exception ex)
        {
            if (_logStream != null)
            {
                _logWriter?.WriteLine(
                    $"{Enum.GetName(typeof(ApiLogLevel), ApiLogLevel.Critical)?.ToUpper()}: Failed to load metadata. Exception: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                _logWriter?.Flush();
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Listener.Stop();
        _logWriter?.Dispose();
        _logStream?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}