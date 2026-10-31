using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace Flow.Launcher.Plugin.WallpaperSearcher
{
    public sealed class SettingsControl : UserControl
    {
        public SettingsControl(
            Settings settings,
            PluginInitContext context,
            Action onSettingsChanged)
        {
            Margin = new Thickness(18, 14, 18, 14);

            var root = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // 1. Wallpaper Folder section
            var folderLabel = new TextBlock
            {
                Text = "Local Wallpaper Folder",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            root.Children.Add(folderLabel);

            var folderGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 14)
            };
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            folderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var folderTextBox = new TextBox
            {
                Text = settings.WallpaperFolder,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 8, 0)
            };
            folderTextBox.TextChanged += (_, _) =>
            {
                settings.WallpaperFolder = folderTextBox.Text.Trim();
                onSettingsChanged();
            };
            Grid.SetColumn(folderTextBox, 0);
            folderGrid.Children.Add(folderTextBox);

            var browseButton = new Button
            {
                Content = "Browse...",
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 6, 0)
            };
            browseButton.Click += (_, _) =>
            {
                using var dialog = new Forms.FolderBrowserDialog
                {
                    Description = "Select your wallpaper folder",
                    UseDescriptionForTitle = true,
                    SelectedPath = Directory.Exists(settings.WallpaperFolder)
                        ? settings.WallpaperFolder
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };

                if (dialog.ShowDialog() == Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    folderTextBox.Text = dialog.SelectedPath;
                }
            };
            Grid.SetColumn(browseButton, 1);
            folderGrid.Children.Add(browseButton);

            var openFolderButton = new Button
            {
                Content = "Open",
                Padding = new Thickness(12, 5, 12, 5)
            };
            openFolderButton.Click += (_, _) =>
            {
                if (Directory.Exists(settings.WallpaperFolder))
                {
                    context.API.OpenDirectory(settings.WallpaperFolder);
                }
                else
                {
                    context.API.ShowMsgError("Folder not found", settings.WallpaperFolder);
                }
            };
            Grid.SetColumn(openFolderButton, 2);
            folderGrid.Children.Add(openFolderButton);

            root.Children.Add(folderGrid);

            // 2. Wallpaper Fit Mode
            var fitRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 14),
                VerticalAlignment = VerticalAlignment.Center
            };

            var fitLabel = new TextBlock
            {
                Text = "Wallpaper Fit:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            fitRow.Children.Add(fitLabel);

            var fitCombo = new ComboBox
            {
                Width = 150,
                ItemsSource = Enum.GetValues<WallpaperFitMode>(),
                SelectedItem = settings.FitMode
            };
            fitCombo.SelectionChanged += (_, _) =>
            {
                if (fitCombo.SelectedItem is WallpaperFitMode selectedFit)
                {
                    settings.FitMode = selectedFit;
                    onSettingsChanged();
                }
            };
            fitRow.Children.Add(fitCombo);
            root.Children.Add(fitRow);

            // 3. Checkboxes
            var subfoldersCheck = new CheckBox
            {
                Content = "Include wallpapers in subfolders",
                IsChecked = settings.IncludeSubfolders,
                Margin = new Thickness(0, 0, 0, 10)
            };
            subfoldersCheck.Checked += (_, _) =>
            {
                settings.IncludeSubfolders = true;
                onSettingsChanged();
            };
            subfoldersCheck.Unchecked += (_, _) =>
            {
                settings.IncludeSubfolders = false;
                onSettingsChanged();
            };
            root.Children.Add(subfoldersCheck);

            var onlineSitesCheck = new CheckBox
            {
                Content = "Show online wallpaper search sites below local wallpapers",
                IsChecked = settings.ShowOnlineWebsites,
                Margin = new Thickness(0, 0, 0, 6)
            };
            onlineSitesCheck.Checked += (_, _) =>
            {
                settings.ShowOnlineWebsites = true;
                onSettingsChanged();
            };
            onlineSitesCheck.Unchecked += (_, _) =>
            {
                settings.ShowOnlineWebsites = false;
                onSettingsChanged();
            };
            root.Children.Add(onlineSitesCheck);

            Content = root;
        }
    }
}
