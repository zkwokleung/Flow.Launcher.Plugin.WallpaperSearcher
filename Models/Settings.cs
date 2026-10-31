using System;
using System.IO;

namespace Flow.Launcher.Plugin.WallpaperSearcher
{
    public enum WallpaperFitMode
    {
        Fill = 0,
        Fit = 1,
        Stretch = 2,
        Tile = 3,
        Center = 4,
        Span = 5
    }

    public class Settings
    {
        public string WallpaperFolder { get; set; } = GetDefaultWallpaperFolder();

        public bool IncludeSubfolders { get; set; } = true;

        public bool ShowOnlineWebsites { get; set; } = true;

        public WallpaperFitMode FitMode { get; set; } = WallpaperFitMode.Fill;

        public string LastAppliedSourcePath { get; set; } = string.Empty;

        public static string GetDefaultWallpaperFolder()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string oneDriveWallpaper = Path.Combine(userProfile, "OneDrive", "Pictures", "wallpaper");
            if (Directory.Exists(oneDriveWallpaper))
            {
                return oneDriveWallpaper;
            }

            string myPictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string picturesWallpaper = Path.Combine(myPictures, "wallpaper");
            if (Directory.Exists(picturesWallpaper))
            {
                return picturesWallpaper;
            }

            return !string.IsNullOrWhiteSpace(myPictures) ? myPictures : oneDriveWallpaper;
        }
    }
}
