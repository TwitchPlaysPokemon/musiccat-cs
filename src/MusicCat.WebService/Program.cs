using System.Text.Json;
using System.Text.Json.Serialization;
using MusicCat;
using MusicCat.Players;
using MusicCat.WebService;

// When we're running as a service, we don't want our working directory to be C:/Windows/system32.
// Having configured paths be relative to the executable file is a more intuitive default.
Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

// Makes it possible to install and run MusicCat as a Windows Service
builder.Services.AddWindowsService(options => options.ServiceName = "MusicCat");

// Our application specific configuration lives in the regular appsettings.json under "MusicCat".
var config = Config.LoadFromConfiguration(builder.Configuration);

builder.Logging.AddConsole();
var loggingConfig = builder.Configuration.GetSection("Logging");
if (loggingConfig.Exists())
    builder.Logging.AddConfiguration(loggingConfig);
var fileLoggingPath = builder.Configuration.GetValue<string>("Logging:LogFilePath");
if (string.IsNullOrEmpty(fileLoggingPath))
    Console.Error.WriteLine("no logging path is configured, logs will only be printed to console");
else
    builder.Logging.AddFile(
        pathFormat: Path.Combine(fileLoggingPath, "musiccat-{Date}.log"),
        outputTemplate: "{Timestamp:o} [{Level:u3}] {Message}{NewLine}{Exception}");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Enums as lowercase strings, so e.g. SongType.Betting becomes "betting".
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(
        namingPolicy: JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
// make swagger-ui our index page since we're just a web service anyway
app.MapGet("/", () => Results.Redirect("/swagger"));

var musicLibrary = MusicCatWebService.GetMusicLibrary(app.Services.GetService<ILogger<MusicLibrary>>()!, config);

MusicCatWebService.AddMusicCatEndpoints(musicLibrary, app);

IPlayer player = new AjaxAMP(config.AjaxAMP, config.WinampPath, config.SongFileDir, musicLibrary);
MusicCatWebService.AddPlayerEndpoints(player, app);

app.Run();
