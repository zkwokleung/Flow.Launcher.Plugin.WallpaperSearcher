using System.Collections.Generic;
using System.IO;
using System.Linq;
using Flow.Launcher.Plugin.SharedCommands;

namespace Flow.Launcher.Plugin.WallpaperSearcher
{
    public class WallpaperSearcher : IPlugin, IPluginI18n
    {
        private PluginInitContext _context;

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            string search = query.Search.Replace(" ", "%20");
            // Check if the query is empty
            if (string.IsNullOrEmpty(query.Search))
            {
                // Show options to open the websites
                return WallpaperWebsites
                    .GetURLs()
                    .Select(url =>
                    {
                        return new Result
                        {
                            Title =
                                $"{_context.API.GetTranslation("plugin_wallpapersearcher_open")} {url.Name}",
                            SubTitle = $"{url.Link}",
                            Action = _ =>
                            {
                                _context.API.OpenUrl(url.Link);
                                return true;
                            },
                            IcoPath = GetWebsiteIconPathOrDefault(url.Name),
                        };
                    })
                    .ToList();
            }

            // Show options to search the query on the websites
            return WallpaperWebsites
                .GetURLs()
                .Select(url =>
                {
                    return new Result
                    {
                        Title =
                            $"{_context.API.GetTranslation("plugin_wallpapersearcher_search")} {url.Name}",
                        SubTitle = $"{url.GetQueryURL(search)}",
                        Action = _ =>
                        {
                            _context.API.OpenUrl(url.GetQueryURL(search));
                            return true;
                        },
                        IcoPath = GetWebsiteIconPathOrDefault(url.Name),
                    };
                })
                .ToList();
        }

        public string GetTranslatedPluginTitle()
        {
            return _context.API.GetTranslation("plugin_wallpapersearcher_plugin_title");
        }

        public string GetTranslatedPluginDescription()
        {
            return _context.API.GetTranslation("plugin_wallpapersearcher_plugin_description");
        }

        public string GetWebsiteIconPathOrDefault(string websiteName)
        {
            string path = $"Images/Logos/{websiteName}.png";

            return File.Exists(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, path)) ? path : $"Images/Logos/website.png";
        }
    }
}
