# MusicCat

MusicCat is a service that reads TPP's [music library](https://github.com/twitchplayspokemon/musiclibrary),
searches for the respective song files on disk, and offers an API to play those on WinAMP using AjaxAMP.
It is a C# rewrite of [musiccat (Python)](https://github.com/twitchPlaysPokemon/musiccat).

## Requirements

- Windows (required for WinAMP)
- .NET 8.0 SDK
- A local copy of the [music library (metadata)](https://github.com/twitchplayspokemon/musiclibrary).
- A local copy of the music library songfiles (not publicly available).
- WinAMP with AjaxAMP plugin (AjaxAMP DLL included under [src/MusicCat/Plugins/](src/MusicCat/Plugins)).
  You need to configure it (WinAMP: Options > Preferences > Plug-ins > General Purpose > AjaxAMP Remote Control Plugin).
  Some defaults work fine, but under "library" you need to configure the song file root path, otherwise nothing plays.

## Setup

### Installation

A GitHub workflow automatically builds the service so you don't need to build it yourself.
You can download the latest artifacts from the [most recent 'build' job on the master branch](https://github.com/TwitchPlaysPokemon/musiccat-cs/actions/workflows/build.yml?query=branch%3Amaster).

If you do want to build MusicCat yourself, follow these steps:

1. Clone the repository
2. Build the project:
   ```shell
   dotnet build
   ```
3. Publish the application for Windows:
   ```shell
   dotnet publish -c Release -r win-x64
   ```
4. The published files will be in `artifacts/publish/MusicCat.WebService/release_win-x64`

### Configuration

You need to put a `appsettings.json` file next to `MusicCat.WebService.exe`.
The [`appsettings.Development.json`](src/MusicCat.WebService/appsettings.Development.json) file can help as a template.

### Running as an Application

Directly execute `MusicCat.WebService.exe`.
If you are a developer, you may also run it as follows:

```shell
dotnet run --project src/MusicCat.WebService
```

### Running as a Windows Service

To install as a Windows service:

1. Run PowerShell as Administrator
2. Run the installation script (located under [scripts/](scripts)):
   ```powershell
   ./Install-MusicCat-Service.ps1
   ```
3. Enter the full path to the MusicCat directory when prompted

To uninstall the service (requires Powershell 6+):

```powershell
./Uninstall-MusicCat-Service.ps1
```

## API Endpoints

MusicCat provides the following API endpoints:

### Music Library Endpoints

- `GET /musiclibrary/verify` - Verifies the music library without loading it
- `POST /musiclibrary/reload` - Reloads the music library from disk
- `GET /musiclibrary/count` - Counts songs in the library, optionally filtered by type (e.g. `?category=betting`)

### Player Endpoints

- `POST /player/launch` - Launch WinAMP if not already running
- `POST /player/play` - Play the current track
- `POST /player/pause` - Pause playback
- `POST /player/stop` - Stop playback
- `POST /player/play/{id}` - Play a specific song by ID
- `POST /player/play-file/{filename}` - Play a specific file
- `GET /player/volume` - Get current volume level
- `PUT /player/volume/{level}` - Set volume level (0.0-1.0)
- `GET /player/position` - Get current playback position
- `PUT /player/position/{pos}` - Set playback position (0.0-1.0)
