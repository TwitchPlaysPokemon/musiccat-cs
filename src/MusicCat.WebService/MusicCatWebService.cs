using System.Text.Json;
using System.Text.Json.Serialization;
using MusicCat.Model;
using MusicCat.Players;

namespace MusicCat.WebService;

public static class MusicCatWebService
{
    public static WebApplication BuildWebApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var config = Config.LoadFromConfiguration(builder.Configuration);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        var loggingConfig = builder.Configuration.GetSection("Logging");
        if (loggingConfig.Exists()) 
            builder.Logging.AddConfiguration(loggingConfig);
        var fileLoggingPath = builder.Configuration.GetValue<string>("Logging:FileLogging:Path");
        if (!string.IsNullOrEmpty(fileLoggingPath))
        {
            builder.Logging.AddFile(
                pathFormat: Path.Combine(fileLoggingPath, "musiccat-{Date}.log"),
                outputTemplate: "{Timestamp:o} [{Level:u3}] {Message}{NewLine}{Exception}");
        }
        else
        {
            Console.Error.WriteLine("no logging path is configured, logs will only be printed to console");
        }
        
        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            // Enums as lowercase strings, so e.g. SongType.Betting becomes "betting".
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(
                namingPolicy: JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
        });

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        var musicLibrary = new MusicLibrary(
            app.Services.GetService<ILogger<MusicLibrary>>()!,
            musiclibraryPath: config.MusicBaseDir!, // TODO should not be nullable
            songfilesPath: config.SongFileDir);
        var loadTask = musicLibrary.Load(); // Start loading asynchronously, we don't need to wait for this to finish.
        loadTask.ContinueWith(task => // But if it fails, we want to know about it
        {
            if (task.IsFaulted) app.Logger.LogError(task.Exception, "Music Library load faulted");
        });

        AddMusicCatEndpoints(musicLibrary, app);
        IPlayer player = new AjaxAMP(config.AjaxAMP, config.WinampPath, config.SongFileDir, musicLibrary);
        AddPlayerEndpoints(player, app);

        return app;
    }

    public static void AddMusicCatEndpoints(MusicLibrary musicLibrary, WebApplication app)
    {
        app.MapGet("/musiclibrary/verify", async (bool reportUnusedSongFiles = false) =>
            {
                IList<string> warnings = await musicLibrary.Verify(reportUnusedSongFiles);
                return string.Join('\n', warnings);
            }).WithDescription("Like /reload, but a dry-run. So it just returns all problems that would occur")
            .WithOpenApi();

        app.MapPost("/musiclibrary/reload", async () =>
            {
                IList<string> warnings = await musicLibrary.Load();
                return string.Join('\n', warnings);
            }).WithDescription("Reloads the entire music library from disk, returning all problems that occurred")
            .WithOpenApi();

        app.MapGet("/musiclibrary/count", async (string? category) =>
            {
                if (category == null)
                    return await musicLibrary.Count(null);
                if (!Enum.TryParse(category, ignoreCase: true,
                        out SongType songType)) // parse manually for case-insensitivity
                    throw new BadHttpRequestException("Unrecognized song type: " + category);
                return await musicLibrary.Count(songType);
            }).WithDescription("Counts all currently enabled songs in the library, optionally filtered to one type")
            .WithOpenApi();
    }

    public static void AddPlayerEndpoints(IPlayer player, WebApplication app)
    {
        app.MapPost("/player/launch",
            async () => await player.Launch());
        app.MapPost("/player/play",
            async () => await player.Play());
        app.MapPost("/player/pause",
            async () => await player.Pause());
        app.MapPost("/player/stop",
            async () => await player.Stop());

        app.MapPost("/player/play/{id}",
            async (string id) => await player.PlayID(id));
        app.MapPost("/player/play-file/{filename}",
            async (string filename) => await player.PlayFile(filename));

        app.MapGet("/player/volume",
            async () => await player.GetVolume());
        app.MapPut("/player/volume/{level:float}", async Task (float level) =>
        {
            if (level is < 0 or > 1)
                throw new BadHttpRequestException("player volume level must be between 0 and 1");
            await player.SetVolume(level);
        });

        app.MapGet("/player/position",
            async () => await player.GetPosition());
        app.MapPut("/player/position/{pos:float}", async Task (float pos) =>
        {
            if (pos is < 0 or > 1)
                throw new BadHttpRequestException("player seek position must be between 0 and 1");
            await player.SetPosition(pos);
        });
    }
}