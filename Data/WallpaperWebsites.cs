using System.Collections.Generic;

namespace Flow.Launcher.Plugin.WallpaperSearcher
{
    public static class WallpaperWebsites
    {
        public static List<URL> GetURLs()
        {
            return new List<URL>()
            {
                new(
                    "Wallpaper Flare",
                    "https://www.wallpaperflare.com/",
                    "search?wallpaper={0}"
                ),
                new("Wallpapers.com", "https://wallpapers.com/", "search/{0}"),
                new("Pexels", "https://www.pexels.com/", "search/{0}/"),
                new("Unsplash", "https://unsplash.com/", "s/photos/{0}")
            };
        }
    }
}
