using System.Text.Json;
using System.Text.Json.Serialization;
using MusicCat;
using MusicCat.Model;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// TODO hook up config
var musicLibrary = new MusicLibrary(
    app.Services.GetService<ILogger<MusicLibrary>>()!,
    musiclibraryPath: "S:/projects/musicLibrary",
    songfilesPath: "V:/musiclibrary");
_ = musicLibrary.Load(); // start loading asynchronously, we don't need to wait for this to finish

app.MapGet("/musiclibrary/verify", async (bool reportUnusedSongFiles = false) =>
{
    IList<string> warnings = await musicLibrary.Verify(reportUnusedSongFiles);
    return string.Join('\n', warnings);
}).WithDescription("Like /reload, but a dry-run. So it just returns all problems that would occur").WithOpenApi();

app.MapGet("/musiclibrary/reload", async () =>
{
    IList<string> warnings = await musicLibrary.Load();
    return string.Join('\n', warnings);
}).WithDescription("Reloads the entire music library from disk, returning all problems that occurred").WithOpenApi();

app.MapGet("/musiclibrary/count", async (string? category) =>
{
    if (category == null)
        return await musicLibrary.Count(null);
    if (!Enum.TryParse(category, ignoreCase: true, out SongType songType)) // parse manually for case-insensitivity
        throw new BadHttpRequestException("Unrecognized song type: " + category);
    return await musicLibrary.Count(songType);
}).WithDescription("Counts all currently enabled songs in the library, optionally filtered to one type").WithOpenApi();

app.Run();