using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Wpf.Ui.Controls;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace OmniGrab;

public partial class MainWindow : FluentWindow
{
    private readonly BinaryManager _binaryManager;
    private readonly DownloaderService _downloaderService;
    private readonly ObservableCollection<DownloadItem> _downloads;
    private CancellationTokenSource? _downloadCts;
    private VideoData? _currentVideoData;

    public MainWindow()
    {
        InitializeComponent();

        _binaryManager = new BinaryManager();
        _downloaderService = new DownloaderService(_binaryManager);
        _downloads = new ObservableCollection<DownloadItem>();

        ListDownloads.ItemsSource = _downloads;
        TxtDownloadFolder.Text = _downloaderService.DownloadDirectory;

        // Hook up binary download notifications
        _binaryManager.ProgressChanged += (msg, pct) =>
        {
            Dispatcher.Invoke(() =>
            {
                TxtFooterStatus.Text = msg;
            });
        };

        UpdateEngineStatusUI();
    }

    private void UpdateEngineStatusUI()
    {
        bool ready = _binaryManager.AreBinariesReady();
        if (ready)
        {
            EngineStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"));
            EngineStatusText.Text = "Core Engine Ready (yt-dlp + FFmpeg Auto-Muxing)";
            BtnUpdateEngine.Content = "Check for Updates";
        }
        else
        {
            EngineStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"));
            EngineStatusText.Text = "Setup Required: Core engine tools not found (Click to setup)";
            BtnUpdateEngine.Content = "⚡ Setup Core Engine";
        }
    }

    private async void BtnUpdateEngine_Click(object sender, RoutedEventArgs e)
    {
        BtnUpdateEngine.IsEnabled = false;
        TxtFooterStatus.Text = "Checking and downloading latest engine tools (yt-dlp & FFmpeg)...";

        try
        {
            await _binaryManager.EnsureBinariesAsync();
            UpdateEngineStatusUI();
            TxtFooterStatus.Text = "Engine tools successfully installed and ready.";
            System.Windows.MessageBox.Show("yt-dlp and FFmpeg processing tools have been successfully configured!", "OmniGrab Core", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            TxtFooterStatus.Text = $"Error installing engine: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to download engine binaries: {ex.Message}", "OmniGrab Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnUpdateEngine.IsEnabled = true;
        }
    }

    private void TxtUrl_TextChanged(object sender, TextChangedEventArgs e)
    {
        string url = TxtUrl.Text.Trim();
        if (!string.IsNullOrEmpty(url))
        {
            string platform = DownloaderService.DetectPlatform(url);
            TxtFooterStatus.Text = $"Detected platform: {platform}";
        }
    }

    private void TxtUrl_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnFetchInfo_Click(sender, e);
        }
    }

    private void BtnPaste_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText().Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    TxtUrl.Text = text;
                    BtnFetchInfo_Click(sender, e);
                }
            }
        }
        catch (Exception ex)
        {
            TxtFooterStatus.Text = $"Clipboard access error: {ex.Message}";
        }
    }

    private async void BtnFetchInfo_Click(object sender, RoutedEventArgs e)
    {
        string url = TxtUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            System.Windows.MessageBox.Show("Please enter or paste a valid video URL.", "OmniGrab", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!_binaryManager.AreBinariesReady())
        {
            TxtFooterStatus.Text = "Setting up engine tools first...";
            await _binaryManager.EnsureBinariesAsync();
            UpdateEngineStatusUI();
        }

        BtnFetchInfo.IsEnabled = false;
        TxtFooterStatus.Text = "Inspecting link and fetching video metadata...";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            _currentVideoData = await _downloaderService.FetchVideoDataAsync(url, cts.Token);

            if (_currentVideoData != null)
            {
                DisplayVideoPreview(_currentVideoData, url);
                TxtFooterStatus.Text = $"Metadata loaded: {_currentVideoData.Title}";
            }
            else
            {
                // Fallback preview based on URL
                string platform = DownloaderService.DetectPlatform(url);
                TxtTitle.Text = url;
                TxtPlatform.Text = platform;
                TxtDuration.Text = "--:--";
                TxtUploader.Text = platform;
                SetPlatformBadgeColor(platform);
                CardPreview.Visibility = Visibility.Visible;
                TxtFooterStatus.Text = "Link verified. Ready to download.";
            }
        }
        catch (Exception ex)
        {
            TxtFooterStatus.Text = $"Could not fetch info: {ex.Message}";
            // Still allow download even if metadata preview had an issue
            string platform = DownloaderService.DetectPlatform(url);
            TxtTitle.Text = url;
            TxtPlatform.Text = platform;
            SetPlatformBadgeColor(platform);
            CardPreview.Visibility = Visibility.Visible;
        }
        finally
        {
            BtnFetchInfo.IsEnabled = true;
        }
    }

    private void DisplayVideoPreview(VideoData data, string originalUrl)
    {
        TxtTitle.Text = data.Title ?? originalUrl;
        TxtUploader.Text = !string.IsNullOrWhiteSpace(data.Uploader) ? data.Uploader : "Unknown Uploader";

        if (data.Duration.HasValue && data.Duration.Value > 0)
        {
            var span = TimeSpan.FromSeconds(data.Duration.Value);
            TxtDuration.Text = span.Hours > 0 ? span.ToString(@"hh\:mm\:ss") : span.ToString(@"mm\:ss");
        }
        else
        {
            TxtDuration.Text = "Stream / Unknown";
        }

        string platform = !string.IsNullOrWhiteSpace(data.ExtractorKey) ? data.ExtractorKey : DownloaderService.DetectPlatform(originalUrl);
        TxtPlatform.Text = platform;
        SetPlatformBadgeColor(platform);

        if (!string.IsNullOrEmpty(data.Thumbnail))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(data.Thumbnail, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ImgThumbnail.Source = bitmap;
            }
            catch { }
        }

        CardPreview.Visibility = Visibility.Visible;
    }

    private void SetPlatformBadgeColor(string platform)
    {
        string colorHex = platform.ToLowerInvariant() switch
        {
            var p when p.Contains("youtube") => "#FF0000",
            var p when p.Contains("tiktok") => "#00F2FE",
            var p when p.Contains("twitch") => "#9146FF",
            var p when p.Contains("reddit") => "#FF4500",
            var p when p.Contains("twitter") || p.Contains("x") => "#1DA1F2",
            var p when p.Contains("instagram") => "#E1306C",
            var p when p.Contains("vimeo") => "#1AB7EA",
            _ => "#1F6FEB"
        };

        BadgePlatform.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
    }

    private void BtnChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Destination Folder for Downloads",
            InitialDirectory = _downloaderService.DownloadDirectory
        };

        if (dialog.ShowDialog() == true)
        {
            _downloaderService.UpdateDownloadDirectory(dialog.FolderName);
            TxtDownloadFolder.Text = dialog.FolderName;
        }
    }

    private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _downloaderService.DownloadDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            TxtFooterStatus.Text = $"Could not open folder: {ex.Message}";
        }
    }

    private async void BtnDownload_Click(object sender, RoutedEventArgs e)
    {
        string url = TxtUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            System.Windows.MessageBox.Show("Please enter a video URL to download.", "OmniGrab", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!_binaryManager.AreBinariesReady())
        {
            TxtFooterStatus.Text = "Configuring core engine before downloading...";
            await _binaryManager.EnsureBinariesAsync();
            UpdateEngineStatusUI();
        }

        string quality = ((ComboBoxItem)CmbQuality.SelectedItem).Content.ToString() ?? "Best Available Quality";
        string title = _currentVideoData?.Title ?? url;
        string platform = _currentVideoData?.ExtractorKey ?? DownloaderService.DetectPlatform(url);

        BtnDownload.IsEnabled = false;
        BtnCancel.IsEnabled = true;
        CardProgress.Visibility = Visibility.Visible;
        ProgressBarDownload.Value = 0;
        TxtProgressPercent.Text = "0%";
        TxtProgressStatus.Text = "Starting download...";
        TxtFooterStatus.Text = "Downloading media streams...";

        AppTaskbarInfo.ProgressState = TaskbarItemProgressState.Normal;
        AppTaskbarInfo.ProgressValue = 0;

        _downloadCts = new CancellationTokenSource();

        var progress = new Progress<DownloadProgress>(p =>
        {
            Dispatcher.Invoke(() =>
            {
                double pct = p.Progress * 100;
                ProgressBarDownload.Value = pct;
                TxtProgressPercent.Text = $"{(int)pct}%";
                AppTaskbarInfo.ProgressValue = p.Progress;

                if (!string.IsNullOrWhiteSpace(p.DownloadSpeed))
                {
                    TxtSpeed.Text = $"Speed: {p.DownloadSpeed}";
                }
                if (!string.IsNullOrWhiteSpace(p.ETA))
                {
                    TxtEta.Text = $"ETA: {p.ETA}";
                }

                if (p.State == DownloadState.PostProcessing)
                {
                    TxtProgressStatus.Text = "Merging audio & video via FFmpeg into MP4...";
                    TxtFooterStatus.Text = "Muxing streams into a single high-quality file...";
                }
                else if (p.State == DownloadState.Downloading)
                {
                    TxtProgressStatus.Text = "Downloading stream data...";
                }
            });
        });

        var logProgress = new Progress<string>(line =>
        {
            Dispatcher.Invoke(() =>
            {
                if (line.Contains("[Merger]", StringComparison.OrdinalIgnoreCase) || line.Contains("Merging", StringComparison.OrdinalIgnoreCase))
                {
                    TxtProgressStatus.Text = "Muxing audio & video via FFmpeg...";
                }
            });
        });

        try
        {
            string? downloadedPath = await _downloaderService.DownloadMediaAsync(url, quality, progress, logProgress, _downloadCts.Token);

            AppTaskbarInfo.ProgressState = TaskbarItemProgressState.None;
            CardProgress.Visibility = Visibility.Collapsed;

            var item = new DownloadItem
            {
                Url = url,
                Title = title,
                Platform = platform,
                Quality = quality,
                DestinationFile = downloadedPath ?? _downloaderService.DownloadDirectory,
                Status = DownloadStatus.Completed,
                StatusMessage = "Finished",
                ProgressPercentage = 100
            };

            _downloads.Insert(0, item);
            TxtNoDownloads.Visibility = Visibility.Collapsed;

            TxtFooterStatus.Text = $"Download complete: {title}";
            System.Windows.MessageBox.Show($"Download finished successfully!\n\nSaved to: {_downloaderService.DownloadDirectory}", "OmniGrab - Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            AppTaskbarInfo.ProgressState = TaskbarItemProgressState.None;
            CardProgress.Visibility = Visibility.Collapsed;
            TxtFooterStatus.Text = "Download cancelled by user.";
        }
        catch (Exception ex)
        {
            AppTaskbarInfo.ProgressState = TaskbarItemProgressState.Error;
            TxtProgressStatus.Text = "Error during download";
            TxtFooterStatus.Text = $"Download failed: {ex.Message}";
            System.Windows.MessageBox.Show($"Download failed:\n{ex.Message}", "OmniGrab Error", MessageBoxButton.OK, MessageBoxImage.Error);
            AppTaskbarInfo.ProgressState = TaskbarItemProgressState.None;
            CardProgress.Visibility = Visibility.Collapsed;
        }
        finally
        {
            BtnDownload.IsEnabled = true;
            BtnCancel.IsEnabled = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        _downloadCts?.Cancel();
        BtnCancel.IsEnabled = false;
        TxtProgressStatus.Text = "Cancelling download...";
    }

    private void BtnPlayVideo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string path && !string.IsNullOrEmpty(path))
        {
            try
            {
                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                }
                else
                {
                    // If exact path is folder or missing, open folder
                    Process.Start(new ProcessStartInfo { FileName = _downloaderService.DownloadDirectory, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                TxtFooterStatus.Text = $"Could not play media: {ex.Message}";
            }
        }
    }

    private void BtnShowFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string path && !string.IsNullOrEmpty(path))
        {
            try
            {
                if (File.Exists(path))
                {
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                else
                {
                    Process.Start("explorer.exe", $"\"{_downloaderService.DownloadDirectory}\"");
                }
            }
            catch (Exception ex)
            {
                TxtFooterStatus.Text = $"Could not show file: {ex.Message}";
            }
        }
    }

    private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
    {
        _downloads.Clear();
        TxtNoDownloads.Visibility = Visibility.Visible;
    }
}