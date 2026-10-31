# Flow.Launcher.Plugin.WallpaperSearcher

A [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher) plugin for browsing, previewing, and applying wallpapers from your local wallpaper folder alongside searching popular online wallpaper websites.

## Features

- **Local Wallpaper Chooser**: Browse and fuzzy-search images (`.jpg`, `.jpeg`, `.png`, `.webp`, `.bmp`, `.jfif`) from your local wallpaper folder with live thumbnails, full preview panel support, image dimensions, file size, and an active wallpaper indicator (`★ Current Wallpaper`).
- **One-Key Desktop Apply**: Press `Enter` to set the selected wallpaper immediately via Windows `IDesktopWallpaper` COM / Win32 API (with automatic lossless `.webp` compatibility handling).
- **Random Wallpaper**: Quickly pick and apply a random wallpaper (`wp random`).
- **Context Menu Actions** (`Shift+Enter` / Right-Click):
  - Set as Desktop Wallpaper (or target individual monitors on multi-monitor setups)
  - Show in File Explorer / Open Image / Open Wallpaper Folder / Copy File Path
- **Settings Panel**: Configure your local wallpaper folder, fit mode (`Fill`, `Fit`, `Stretch`, `Tile`, `Center`, `Span`), subfolder scanning, and whether to display online wallpaper sites below local results.
- **Online Wallpaper Search**: Search or open Wallpaper Flare, Wallpapers.com, Pexels, Unsplash, Wallhaven, and Wallpaper Craft.

## Usage

```text
wp
wp <keywords>
wp random
```

## Build & Local Deploy

```powershell
.\deploy.ps1
```
