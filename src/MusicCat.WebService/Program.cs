using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using MusicCat;
using MusicCat.Players;
using MusicCat.WebService;

var builder = WebApplication.CreateBuilder(args);

// Makes it possible to install and run MusicCat as a Windows Service
builder.Services.AddWindowsService(options => options.ServiceName = "MusicCat");

builder.Services.AddResponseCompression();

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

// Enums as lowercase strings, so e.g. SongType.Betting becomes "betting".
var jsonStringEnumConverter = new JsonStringEnumConverter(
    namingPolicy: JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false);
builder.Services.ConfigureHttpJsonOptions(options => 
    options.SerializerOptions.Converters.Add(jsonStringEnumConverter));
// Needs to be specified a second time to work around https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2293
builder.Services.Configure<JsonOptions>(options => 
    options.JsonSerializerOptions.Converters.Add(jsonStringEnumConverter));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MusicCat.WebService", Version = "v1" });
    options.SupportNonNullableReferenceTypes();
    options.NonNullableReferenceTypesAsRequired();
});

WebApplication app = builder.Build();

app.UseResponseCompression();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // If we're visiting the Swagger-UI, we're typically testing stuff.
    // Having to first click "Try it out" for every operation is just an unnecessary step.
    options.EnableTryItOutByDefault();
});
// make swagger-ui our index page since we're just a web service anyway
app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription(); // avoid this becoming a documented official endpoint

var musicLibrary = MusicCatWebService.GetMusicLibrary(app.Services.GetService<ILogger<MusicLibrary>>()!, config);

MusicCatWebService.AddMusicCatEndpoints(musicLibrary, app);

IPlayer player = new AjaxAMP(config.AjaxAMP, config.WinampPath, config.SongFileDir, musicLibrary);
MusicCatWebService.AddPlayerEndpoints(player, app);

app.Run();
