using System.Collections.Generic;
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
                return WallpaperWebsites
                    .GetURLs()
                    .Select(url =>
                    {
                        return new Result
                        {
                            Title =
                                $"{_context.API.GetTranslation("plugin_wallpapersearcher_open")} {url.Name}",
                            SubTitle = $"{url.Link}",
                            Action = c =>
                            {
                                _context.API.OpenUrl(url.Link);
                                return true;
                            },
                            IcoPath = $"Images/Logos/{url.Name}.png"
                        };
                    })
                    .ToList();
            }

            return WallpaperWebsites
                .GetURLs()
                .Select(url =>
                {
                    return new Result
                    {
                        Title =
                            $"{_context.API.GetTranslation("plugin_wallpapersearcher_search")} {url.Name}",
                        SubTitle = $"{url.GetQueryURL(search)}",
                        Action = c =>
                        {
                            _context.API.OpenUrl(url.GetQueryURL(search));
                            return true;
                        },
                        IcoPath = $"Images/Logos/{url.Name}.png"
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
    }
}
