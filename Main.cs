using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.WallpaperSearcher
{
    public class WallpaperSearcher
        : IPlugin,
          IPluginI18n,
          IContextMenu,
          ISettingProvider,
          IReloadable
    {
        private PluginInitContext _context = null!;
        private Settings _settings = null!;
        private WallpaperService _wallpaperService = null!;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            if (string.IsNullOrWhiteSpace(_settings.WallpaperFolder))
            {
                _settings.WallpaperFolder = Settings.GetDefaultWallpaperFolder();
                _context.API.SaveSettingJsonStorage<Settings>();
            }

            _wallpaperService = new WallpaperService();
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            string rawSearch = query.Search?.Trim() ?? string.Empty;

            if (!Directory.Exists(_settings.WallpaperFolder))
            {
                results.Add(new Result
                {
                    Title = "Wallpaper folder not found — click to configure",
                    SubTitle = _settings.WallpaperFolder,
                    IcoPath = "Images/icon.png",
                    Score = 1000,
                    Action = _ =>
                    {
                        _context.API.OpenSettingDialog();
                        return true;
                    }
                });
            }
            else
            {
                List<WallpaperItem> localWallpapers = _wallpaperService.GetWallpapers(_settings);
                string activePath = _wallpaperService.GetActiveWallpaperPath(_settings);

                if (string.IsNullOrEmpty(rawSearch))
                {
                    for (int i = 0; i < localWallpapers.Count; i++)
                    {
                        WallpaperItem item = localWallpapers[i];
                        bool isCurrent = string.Equals(item.FullPath, activePath, StringComparison.OrdinalIgnoreCase);
                        int score = Math.Max(200, 500 - i);
                        results.Add(CreateWallpaperResult(item, isCurrent, score, highlightData: null));
                    }

                    if (localWallpapers.Count > 0)
                    {
                        results.Add(CreateRandomWallpaperResult(localWallpapers, activePath, score: 180));
                    }
                }
                else
                {
                    if (localWallpapers.Count > 0 &&
                        ("random wallpaper".Contains(rawSearch, StringComparison.OrdinalIgnoreCase) ||
                         _context.API.FuzzySearch(rawSearch, "Random Wallpaper").IsSearchPrecisionScoreMet()))
                    {
                        results.Add(CreateRandomWallpaperResult(localWallpapers, activePath, score: 950));
                    }

                    foreach (WallpaperItem item in localWallpapers)
                    {
                        var fuzzy = _context.API.FuzzySearch(rawSearch, item.Title);
                        bool titleSubstring = item.Title.Contains(rawSearch, StringComparison.OrdinalIgnoreCase);
                        bool metaSubstring = item.ExtensionLabel.Contains(rawSearch, StringComparison.OrdinalIgnoreCase)
                            || item.DimensionsText.Contains(rawSearch, StringComparison.OrdinalIgnoreCase);

                        if (!fuzzy.IsSearchPrecisionScoreMet() && !titleSubstring && !metaSubstring)
                        {
                            continue;
                        }

                        bool isCurrent = string.Equals(item.FullPath, activePath, StringComparison.OrdinalIgnoreCase);
                        int matchScore = 300 + Math.Max(0, fuzzy.Score);
                        if (titleSubstring)
                        {
                            matchScore += 120;
                        }
                        else if (metaSubstring)
                        {
                            matchScore += 60;
                        }

                        IList<int>? highlights = fuzzy.IsSearchPrecisionScoreMet() ? fuzzy.MatchData : null;
                        results.Add(CreateWallpaperResult(item, isCurrent, matchScore, highlights));
                    }
                }
            }

            if (_settings.ShowOnlineWebsites)
            {
                AppendOnlineWebsiteResults(results, query.Search);
            }

            return results;
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.ContextData is not WallpaperItem item)
            {
                return new List<Result>();
            }

            var menu = new List<Result>
            {
                new()
                {
                    Title = "Set as Desktop Wallpaper",
                    SubTitle = $"Apply '{item.Title}' ({_settings.FitMode})",
                    IcoPath = "Images/icon.png",
                    Action = _ => ApplyWallpaperAction(item.FullPath, item.Title)
                }
            };

            IReadOnlyList<string> monitors = _wallpaperService.GetMonitorIds();
            if (monitors.Count > 1)
            {
                for (int i = 0; i < monitors.Count; i++)
                {
                    int monitorNumber = i + 1;
                    string monitorId = monitors[i];
                    menu.Add(new Result
                    {
                        Title = $"Set on Monitor {monitorNumber} Only",
                        SubTitle = item.Title,
                        IcoPath = "Images/icon.png",
                        Action = _ => ApplyWallpaperAction(item.FullPath, $"{item.Title} (Monitor {monitorNumber})", monitorId)
                    });
                }
            }

            menu.Add(new Result
            {
                Title = "Show in File Explorer",
                SubTitle = item.FullPath,
                IcoPath = "Images/icon.png",
                Action = _ =>
                {
                    string? dir = Path.GetDirectoryName(item.FullPath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        _context.API.OpenDirectory(dir, item.FullPath);
                    }

                    return true;
                }
            });

            menu.Add(new Result
            {
                Title = "Open Image",
                SubTitle = "Open in default image viewer",
                IcoPath = "Images/icon.png",
                Action = _ =>
                {
                    _context.API.OpenUrl(item.FullPath);
                    return true;
                }
            });

            menu.Add(new Result
            {
                Title = "Open Wallpaper Folder",
                SubTitle = _settings.WallpaperFolder,
                IcoPath = "Images/icon.png",
                Action = _ =>
                {
                    if (Directory.Exists(_settings.WallpaperFolder))
                    {
                        _context.API.OpenDirectory(_settings.WallpaperFolder);
                    }

                    return true;
                }
            });

            menu.Add(new Result
            {
                Title = "Copy File Path",
                SubTitle = item.FullPath,
                IcoPath = "Images/icon.png",
                Action = _ =>
                {
                    _context.API.CopyToClipboard(item.FullPath);
                    return true;
                }
            });

            return menu;
        }

        public Control CreateSettingPanel()
        {
            return new SettingsControl(
                _settings,
                _context,
                () =>
                {
                    _wallpaperService.ClearCache();
                    _context.API.SaveSettingJsonStorage<Settings>();
                }
            );
        }

        public void ReloadData()
        {
            _wallpaperService.ClearCache();
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
            return File.Exists(Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, path))
                ? path
                : "Images/Logos/website.png";
        }

        private Result CreateWallpaperResult(
            WallpaperItem item,
            bool isCurrent,
            int score,
            IList<int>? highlightData)
        {
            string summary = item.BuildMetadataSummary(isCurrent);
            return new Result
            {
                Title = item.Title,
                SubTitle = $"{summary} — Press Enter to set as wallpaper",
                IcoPath = item.FullPath,
                RoundedIcon = true,
                Score = score,
                TitleHighlightData = highlightData,
                CopyText = item.FullPath,
                ContextData = item,
                Preview = new Result.PreviewInfo
                {
                    PreviewImagePath = item.FullPath,
                    IsMedia = true,
                    Description = summary
                },
                Action = _ => ApplyWallpaperAction(item.FullPath, item.Title)
            };
        }

        private Result CreateRandomWallpaperResult(
            List<WallpaperItem> localWallpapers,
            string activePath,
            int score)
        {
            return new Result
            {
                Title = "Random Wallpaper",
                SubTitle = $"Pick and apply a random wallpaper from {localWallpapers.Count} images in your folder",
                IcoPath = "Images/icon.png",
                Score = score,
                Action = _ =>
                {
                    List<WallpaperItem> candidates = localWallpapers.Count > 1
                        ? localWallpapers
                            .Where(w => !string.Equals(w.FullPath, activePath, StringComparison.OrdinalIgnoreCase))
                            .ToList()
                        : localWallpapers;

                    if (candidates.Count == 0)
                    {
                        candidates = localWallpapers;
                    }

                    WallpaperItem chosen = candidates[Random.Shared.Next(candidates.Count)];
                    return ApplyWallpaperAction(chosen.FullPath, chosen.Title);
                }
            };
        }

        private bool ApplyWallpaperAction(string fullPath, string title, string? monitorId = null)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    _wallpaperService.SetDesktopWallpaper(fullPath, _settings, monitorId);
                    _context.API.SaveSettingJsonStorage<Settings>();
                }
                catch (Exception ex)
                {
                    _context.API.LogException(nameof(WallpaperSearcher), "Failed to set desktop wallpaper", ex);
                    _context.API.ShowMsgError("Failed to set wallpaper", $"{title}: {ex.Message}");
                }
            });

            return true;
        }

        private void AppendOnlineWebsiteResults(List<Result> results, string? rawQuery)
        {
            if (string.IsNullOrWhiteSpace(rawQuery))
            {
                foreach (URL url in WallpaperWebsites.GetURLs())
                {
                    results.Add(new Result
                    {
                        Title = $"{_context.API.GetTranslation("plugin_wallpapersearcher_open")} {url.Name}",
                        SubTitle = url.Link,
                        Score = 10,
                        IcoPath = GetWebsiteIconPathOrDefault(url.Name),
                        Action = _ =>
                        {
                            _context.API.OpenUrl(url.Link);
                            return true;
                        }
                    });
                }
            }
            else
            {
                string encodedSearch = Uri.EscapeDataString(rawQuery.Trim());
                foreach (URL url in WallpaperWebsites.GetURLs())
                {
                    string queryUrl = url.GetQueryURL(encodedSearch);
                    results.Add(new Result
                    {
                        Title = $"{_context.API.GetTranslation("plugin_wallpapersearcher_search")} {url.Name}",
                        SubTitle = queryUrl,
                        Score = 10,
                        IcoPath = GetWebsiteIconPathOrDefault(url.Name),
                        Action = _ =>
                        {
                            _context.API.OpenUrl(queryUrl);
                            return true;
                        }
                    });
                }
            }
        }
    }
}
