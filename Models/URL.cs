using System;

namespace Flow.Launcher.Plugin.WallpaperSearcher
{
    public class URL
    {
        public string Name { get; set; }
        public string Link { get; set; }
        public string Query { get; set; }

        public URL(string name, string link, string query)
        {
            Name = name;
            Link = link;
            Query = query;
        }

        public string GetQueryURL(string query)
        {
            return string.Format($"{Link}{Query}", query);
        }
    }
}
