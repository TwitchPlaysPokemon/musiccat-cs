$service_path = Read-Host 'What is the full path to MusicCat (without executable name or trailing slash)?'

New-Service `
  -Name MusicCat `
  -BinaryPathName "$service_path/MusicCat.WebService.exe --contentRoot $service_path" `
  -DisplayName "MusicCat" `
  -Description "TPP music web service that reads the music library and controls WinAMP" `
  -StartupType Automatic
