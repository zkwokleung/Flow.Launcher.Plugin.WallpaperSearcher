dotnet publish . -c Release -r win-x64 --no-self-contained
Compress-Archive -LiteralPath bin/Release/win-x64/publish -DestinationPath bin/WallpaperSearcher.zip -Force