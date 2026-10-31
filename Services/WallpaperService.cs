using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Flow.Launcher.Plugin.WallpaperSearcher
{
    public sealed class WallpaperService
    {
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;
        private const FileAttributes FileAttributeRecallOnDataAccess = (FileAttributes)0x00400000;

        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".bmp",
            ".jfif"
        };

        private readonly ConcurrentDictionary<string, WallpaperItem> _itemCache = new(StringComparer.OrdinalIgnoreCase);

        public void ClearCache()
        {
            _itemCache.Clear();
        }

        public List<WallpaperItem> GetWallpapers(Settings settings)
        {
            string folder = settings.WallpaperFolder?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return new List<WallpaperItem>();
            }

            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = settings.IncludeSubfolders,
                ReturnSpecialDirectories = false
            };

            var items = new List<WallpaperItem>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(folder, "*.*", options);
            }
            catch
            {
                return items;
            }

            foreach (string filePath in files)
            {
                string ext = Path.GetExtension(filePath);
                if (string.IsNullOrEmpty(ext) || !SupportedExtensions.Contains(ext))
                {
                    continue;
                }

                seenPaths.Add(filePath);

                try
                {
                    var info = new FileInfo(filePath);
                    if (!info.Exists)
                    {
                        continue;
                    }

                    if (_itemCache.TryGetValue(filePath, out WallpaperItem? cached) &&
                        cached.LastWriteTimeUtc == info.LastWriteTimeUtc &&
                        cached.FileSizeBytes == info.Length)
                    {
                        items.Add(cached);
                        continue;
                    }

                    (int width, int height) = TryReadDimensionsIfLocal(info);

                    var item = new WallpaperItem
                    {
                        FullPath = info.FullName,
                        Title = Path.GetFileNameWithoutExtension(info.Name),
                        ExtensionLabel = ext.TrimStart('.').ToUpperInvariant(),
                        FileSizeBytes = info.Length,
                        LastWriteTimeUtc = info.LastWriteTimeUtc,
                        Width = width,
                        Height = height
                    };

                    _itemCache[filePath] = item;
                    items.Add(item);
                }
                catch
                {
                    // Skip unreadable individual files gracefully
                }
            }

            foreach (string cachedKey in _itemCache.Keys)
            {
                if (!seenPaths.Contains(cachedKey))
                {
                    _itemCache.TryRemove(cachedKey, out _);
                }
            }

            return items
                .OrderByDescending(i => i.LastWriteTimeUtc)
                .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public string GetActiveWallpaperPath(Settings settings)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: false);
                string? regWallpaper = key?.GetValue("WallPaper") as string
                    ?? key?.GetValue("Wallpaper") as string;

                if (!string.IsNullOrWhiteSpace(regWallpaper))
                {
                    string cacheDir = GetCacheDirectory();
                    if (regWallpaper.StartsWith(cacheDir, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(settings.LastAppliedSourcePath) &&
                        File.Exists(settings.LastAppliedSourcePath))
                    {
                        return settings.LastAppliedSourcePath;
                    }

                    return regWallpaper;
                }
            }
            catch
            {
                // Ignore registry read issues
            }

            return settings.LastAppliedSourcePath ?? string.Empty;
        }

        public IReadOnlyList<string> GetMonitorIds()
        {
            return RunOnStaThread<IReadOnlyList<string>>(() =>
            {
                var result = new List<string>();
                IDesktopWallpaper? desktopWallpaper = null;
                try
                {
                    desktopWallpaper = CreateDesktopWallpaperCom();
                    if (desktopWallpaper == null)
                    {
                        return result;
                    }

                    uint count = desktopWallpaper.GetMonitorDevicePathCount();
                    for (uint i = 0; i < count; i++)
                    {
                        string id = desktopWallpaper.GetMonitorDevicePathAt(i);
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            result.Add(id);
                        }
                    }
                }
                catch
                {
                    // Ignore COM enumeration errors
                }
                finally
                {
                    if (desktopWallpaper != null && Marshal.IsComObject(desktopWallpaper))
                    {
                        Marshal.ReleaseComObject(desktopWallpaper);
                    }
                }

                return result;
            });
        }

        public void SetDesktopWallpaper(string sourceImagePath, Settings settings, string? monitorId = null)
        {
            if (string.IsNullOrWhiteSpace(sourceImagePath) || !File.Exists(sourceImagePath))
            {
                throw new FileNotFoundException("Wallpaper image file was not found.", sourceImagePath);
            }

            RunOnStaThread<bool>(() =>
            {
                string effectivePath = PrepareCompatibleWallpaperFile(sourceImagePath);
                ApplyRegistryWallpaperFit(settings.FitMode);

                bool appliedViaCom = TryApplyViaDesktopWallpaperCom(effectivePath, settings.FitMode, monitorId);
                if (!appliedViaCom)
                {
                    int ok = SystemParametersInfoW(
                        SPI_SETDESKWALLPAPER,
                        0,
                        effectivePath,
                        SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE
                    );

                    if (ok == 0)
                    {
                        int win32Err = Marshal.GetLastWin32Error();
                        throw new InvalidOperationException($"SystemParametersInfo failed with error code {win32Err}.");
                    }
                }

                return true;
            });

            settings.LastAppliedSourcePath = sourceImagePath;
        }

        private static (int Width, int Height) TryReadDimensionsIfLocal(FileInfo info)
        {
            try
            {
                FileAttributes attrs = info.Attributes;
                if ((attrs & FileAttributes.Offline) != 0 || (attrs & FileAttributeRecallOnDataAccess) != 0)
                {
                    return (0, 0);
                }

                using FileStream stream = new(info.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                BitmapDecoder decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.DelayCreation,
                    BitmapCacheOption.None
                );
                if (decoder.Frames.Count > 0)
                {
                    return (decoder.Frames[0].PixelWidth, decoder.Frames[0].PixelHeight);
                }
            }
            catch
            {
                // Dimension metadata is optional
            }

            return (0, 0);
        }

        private static string PrepareCompatibleWallpaperFile(string sourcePath)
        {
            string ext = Path.GetExtension(sourcePath);
            if (!string.Equals(ext, ".webp", StringComparison.OrdinalIgnoreCase))
            {
                return sourcePath;
            }

            string cacheDir = GetCacheDirectory();
            Directory.CreateDirectory(cacheDir);

            byte[] hashBytes = SHA256.HashData(
                Encoding.UTF8.GetBytes($"{sourcePath}|{File.GetLastWriteTimeUtc(sourcePath).Ticks}")
            );
            string shortHash = Convert.ToHexString(hashBytes)[..12].ToLowerInvariant();
            string targetPngPath = Path.Combine(cacheDir, $"converted_{shortHash}.png");

            if (!File.Exists(targetPngPath))
            {
                using FileStream input = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                BitmapDecoder decoder = BitmapDecoder.Create(
                    input,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad
                );

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0]));

                using FileStream output = new(targetPngPath, FileMode.Create, FileAccess.Write, FileShare.None);
                encoder.Save(output);
            }

            try
            {
                foreach (string oldFile in Directory.EnumerateFiles(cacheDir, "converted_*.png"))
                {
                    if (!string.Equals(oldFile, targetPngPath, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            File.Delete(oldFile);
                        }
                        catch
                        {
                            // In use by shell; ignore
                        }
                    }
                }
            }
            catch
            {
                // Ignore cleanup issues
            }

            return targetPngPath;
        }

        private static string GetCacheDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlowWallpaperSearcher"
            );
        }

        private static void ApplyRegistryWallpaperFit(WallpaperFitMode fitMode)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true);
                if (key == null)
                {
                    return;
                }

                (string style, string tile) = fitMode switch
                {
                    WallpaperFitMode.Fill => ("10", "0"),
                    WallpaperFitMode.Fit => ("6", "0"),
                    WallpaperFitMode.Stretch => ("2", "0"),
                    WallpaperFitMode.Tile => ("0", "1"),
                    WallpaperFitMode.Center => ("0", "0"),
                    WallpaperFitMode.Span => ("22", "0"),
                    _ => ("10", "0")
                };

                key.SetValue("WallpaperStyle", style, RegistryValueKind.String);
                key.SetValue("TileWallpaper", tile, RegistryValueKind.String);
            }
            catch
            {
                // Registry update is best-effort alongside COM/Win32 API
            }
        }

        private static bool TryApplyViaDesktopWallpaperCom(
            string effectivePath,
            WallpaperFitMode fitMode,
            string? monitorId)
        {
            IDesktopWallpaper? desktopWallpaper = null;
            try
            {
                desktopWallpaper = CreateDesktopWallpaperCom();
                if (desktopWallpaper == null)
                {
                    return false;
                }

                DesktopWallpaperPosition pos = fitMode switch
                {
                    WallpaperFitMode.Center => DesktopWallpaperPosition.Center,
                    WallpaperFitMode.Tile => DesktopWallpaperPosition.Tile,
                    WallpaperFitMode.Stretch => DesktopWallpaperPosition.Stretch,
                    WallpaperFitMode.Fit => DesktopWallpaperPosition.Fit,
                    WallpaperFitMode.Fill => DesktopWallpaperPosition.Fill,
                    WallpaperFitMode.Span => DesktopWallpaperPosition.Span,
                    _ => DesktopWallpaperPosition.Fill
                };

                desktopWallpaper.SetPosition(pos);
                desktopWallpaper.SetWallpaper(monitorId, effectivePath);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (desktopWallpaper != null && Marshal.IsComObject(desktopWallpaper))
                {
                    Marshal.ReleaseComObject(desktopWallpaper);
                }
            }
        }

        private static IDesktopWallpaper? CreateDesktopWallpaperCom()
        {
            var clsid = new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");
            Type? comType = Type.GetTypeFromCLSID(clsid, throwOnError: false);
            if (comType == null)
            {
                return null;
            }

            return Activator.CreateInstance(comType) as IDesktopWallpaper;
        }

        private static T RunOnStaThread<T>(Func<T> func)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return func();
            }

            T? result = default;
            Exception? captured = null;

            var thread = new Thread(() =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    captured = ex;
                }
            });

            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (captured != null)
            {
                throw new InvalidOperationException(captured.Message, captured);
            }

            return result!;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int SystemParametersInfoW(
            int uAction,
            int uParam,
            string lpvParam,
            int fuWinIni
        );

        private enum DesktopWallpaperPosition
        {
            Center = 0,
            Tile = 1,
            Stretch = 2,
            Fit = 3,
            Fill = 4,
            Span = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [ComImport]
        [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDesktopWallpaper
        {
            void SetWallpaper(
                [MarshalAs(UnmanagedType.LPWStr)] string? monitorID,
                [MarshalAs(UnmanagedType.LPWStr)] string wallpaper
            );

            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string? monitorID);

            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetMonitorDevicePathAt(uint monitorIndex);

            uint GetMonitorDevicePathCount();

            Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

            void SetBackgroundColor(uint color);

            uint GetBackgroundColor();

            void SetPosition(DesktopWallpaperPosition position);

            DesktopWallpaperPosition GetPosition();
        }
    }
}
