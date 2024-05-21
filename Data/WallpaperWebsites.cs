using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.WallpaperSearcher
{
    public static class WallpaperWebsites
    {
        public static List<URL> GetURLs()
        {
            return new List<URL>()
            {
                new URL(
                    "Wallpaper Flare",
                    "https://www.wallpaperflare.com/",
                    "search?wallpaper={0}"
                ),
                new URL("Wallpapers.com", "https://wallpapers.com/", "search/{0}"),
                new URL("Pexels", "https://www.pexels.com/", "search/{0}/")
            };
        }
    }
}
