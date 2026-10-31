using System;

namespace Flow.Launcher.Plugin.WallpaperSearcher
{
    public sealed class WallpaperItem
    {
        public required string FullPath { get; init; }

        public required string Title { get; init; }

        public required string ExtensionLabel { get; init; }

        public required long FileSizeBytes { get; init; }

        public required DateTime LastWriteTimeUtc { get; init; }

        public int Width { get; init; }

        public int Height { get; init; }

        public string FormattedSize => FormatBytes(FileSizeBytes);

        public string DimensionsText =>
            Width > 0 && Height > 0 ? $"{Width} × {Height}" : string.Empty;

        public string BuildMetadataSummary(bool isCurrent)
        {
            string prefix = isCurrent ? "★ Current Wallpaper • " : string.Empty;
            string dims = !string.IsNullOrEmpty(DimensionsText) ? $"{DimensionsText} • " : string.Empty;
            return $"{prefix}{dims}{ExtensionLabel} • {FormattedSize}";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / (1024.0 * 1024.0):0.00} MB";
            }

            if (bytes >= 1024)
            {
                return $"{bytes / 1024.0:0} KB";
            }

            return $"{bytes} B";
        }
    }
}
