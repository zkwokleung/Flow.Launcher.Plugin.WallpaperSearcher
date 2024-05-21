using System;
using System.Collections.Generic;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.WallpaperSearcher
{
    public class WallpaperSearcher : IPlugin
    {
        private PluginInitContext _context;

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            return new List<Result>();
        }
    }
}