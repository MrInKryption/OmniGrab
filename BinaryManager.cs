using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OmniGrab;

public class BinaryManager
{
    private readonly HttpClient _httpClient;
    public string BinariesDirectory { get; }
    public string YtDlpPath => Path.Combine(BinariesDirectory, "yt-dlp.exe");
    public string FFmpegPath => Path.Combine(BinariesDirectory, "ffmpeg.exe");
    public string FFprobePath => Path.Combine(BinariesDirectory, "ffprobe.exe");

    public event Action<string, double>? ProgressChanged;

    public BinaryManager()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OmniGrab-Downloader/1.0");

        // Keep binaries in AppData to ensure write permissions across all environments
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        BinariesDirectory = Path.Combine(appData, "OmniGrab", "bin");
        Directory.CreateDirectory(BinariesDirectory);
    }

    public bool AreBinariesReady()
    {
        return File.Exists(YtDlpPath) && File.Exists(FFmpegPath);
    }

    public async Task EnsureBinariesAsync(CancellationToken ct = default)
    {
        if (!File.Exists(YtDlpPath))
        {
            await DownloadYtDlpAsync(ct);
        }

        if (!File.Exists(FFmpegPath))
        {
            await DownloadFFmpegAsync(ct);
        }
    }

    public async Task DownloadYtDlpAsync(CancellationToken ct = default)
    {
        string url = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
        ProgressChanged?.Invoke("Downloading yt-dlp core engine...", 0);

        string tempPath = YtDlpPath + ".tmp";
        await DownloadFileWithProgressAsync(url, tempPath, ct);

        if (File.Exists(YtDlpPath))
        {
            File.Delete(YtDlpPath);
        }
        File.Move(tempPath, YtDlpPath);

        ProgressChanged?.Invoke("yt-dlp core installed successfully.", 100);
    }

    public async Task DownloadFFmpegAsync(CancellationToken ct = default)
    {
        string url = "https://github.com/yt-dlp/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-win64-gpl.zip";
        ProgressChanged?.Invoke("Downloading FFmpeg audio/video merger...", 0);

        string tempZip = Path.Combine(BinariesDirectory, "ffmpeg_download.zip");
        await DownloadFileWithProgressAsync(url, tempZip, ct);

        ProgressChanged?.Invoke("Extracting FFmpeg binaries...", 90);

        string extractTemp = Path.Combine(BinariesDirectory, "ffmpeg_extracted");
        if (Directory.Exists(extractTemp))
        {
            Directory.Delete(extractTemp, true);
        }

        ZipFile.ExtractToDirectory(tempZip, extractTemp);

        // Find ffmpeg.exe and ffprobe.exe inside the extracted folder structure
        string[] foundFiles = Directory.GetFiles(extractTemp, "*.exe", SearchOption.AllDirectories);
        foreach (var file in foundFiles)
        {
            string fileName = Path.GetFileName(file).ToLowerInvariant();
            if (fileName == "ffmpeg.exe" || fileName == "ffprobe.exe")
            {
                string target = Path.Combine(BinariesDirectory, fileName);
                File.Copy(file, target, true);
            }
        }

        // Cleanup temp extraction files
        try
        {
            Directory.Delete(extractTemp, true);
            File.Delete(tempZip);
        }
        catch { }

        ProgressChanged?.Invoke("FFmpeg installed successfully.", 100);
    }

    private async Task DownloadFileWithProgressAsync(string url, string destinationPath, CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;

        using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        byte[] buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
            totalRead += bytesRead;

            if (totalBytes.HasValue && totalBytes.Value > 0)
            {
                double percentage = (double)totalRead / totalBytes.Value * 100;
                ProgressChanged?.Invoke($"Downloading ({totalRead / 1024 / 1024:F1} MB / {totalBytes.Value / 1024 / 1024:F1} MB)", percentage);
            }
        }
    }
}
