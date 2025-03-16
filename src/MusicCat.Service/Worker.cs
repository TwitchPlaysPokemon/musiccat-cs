using MusicCat.WebService;

namespace MusicCat.Service;

public class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var app = MusicCatWebService.BuildWebApplication([]);
        await app.RunAsync(stoppingToken);
    }
}
