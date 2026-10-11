using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace OmniGrab;

public class DownloaderService
{
    private readonly YoutubeDL _ytdl;
    private readonly BinaryManager _binaryManager;

    public BinaryManager BinaryManager => _binaryManager;
    public string DownloadDirectory { get; set; }

    public DownloaderService(BinaryManager binaryManager)
    {
        _binaryManager = binaryManager;
        DownloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        _ytdl = new YoutubeDL
        {
            YoutubeDLPath = _binaryManager.YtDlpPath,
            FFmpegPath = _binaryManager.FFmpegPath,
            OutputFolder = DownloadDirectory,
            OutputFileTemplate = "%(title)s.%(ext)s"
        };
    }

    public void UpdateDownloadDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            DownloadDirectory = path;
            _ytdl.OutputFolder = path;
        }
    }

    public static string DetectPlatform(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "Unknown";
        string lower = url.ToLowerInvariant();

        if (lower.Contains("youtube.com") || lower.Contains("youtu.be")) return "YouTube";
        if (lower.Contains("tiktok.com")) return "TikTok";
        if (lower.Contains("twitch.tv")) return "Twitch";
        if (lower.Contains("reddit.com") || lower.Contains("v.redd.it")) return "Reddit";
        if (lower.Contains("twitter.com") || lower.Contains("x.com")) return "X / Twitter";
        if (lower.Contains("instagram.com")) return "Instagram";
        if (lower.Contains("vimeo.com")) return "Vimeo";
        if (lower.Contains("kick.com")) return "Kick";
        if (lower.Contains("facebook.com") || lower.Contains("fb.watch")) return "Facebook";

        return "Web Media";
    }

    public async Task<VideoData?> FetchVideoDataAsync(string url, CancellationToken ct = default)
    {
        _ytdl.YoutubeDLPath = _binaryManager.YtDlpPath;
        _ytdl.FFmpegPath = _binaryManager.FFmpegPath;

        var result = await _ytdl.RunVideoDataFetch(url, ct);
        if (result.Success)
        {
            return result.Data;
        }

        return null;
    }

    public async Task<string?> DownloadMediaAsync(
        string url,
        string qualityPreset,
        IProgress<DownloadProgress> progress,
        IProgress<string> outputLog,
        CancellationToken ct = default)
    {
        _ytdl.YoutubeDLPath = _binaryManager.YtDlpPath;
        _ytdl.FFmpegPath = _binaryManager.FFmpegPath;
        _ytdl.OutputFolder = DownloadDirectory;

        if (qualityPreset.Contains("MP3", StringComparison.OrdinalIgnoreCase))
        {
            var audioResult = await _ytdl.RunAudioDownload(
                url,
                AudioConversionFormat.Mp3,
                ct,
                progress,
                outputLog);

            return audioResult.Success ? audioResult.Data : null;
        }
        else if (qualityPreset.Contains("M4A", StringComparison.OrdinalIgnoreCase))
        {
            var audioResult = await _ytdl.RunAudioDownload(
                url,
                AudioConversionFormat.M4a,
                ct,
                progress,
                outputLog);

            return audioResult.Success ? audioResult.Data : null;
        }
        else
        {
            // Video download with guaranteed auto-merge into MP4 via FFmpeg
            string formatSelector = "bv*+ba/b";

            if (qualityPreset.Contains("1080p", StringComparison.OrdinalIgnoreCase))
            {
                formatSelector = "bv*[height<=1080]+ba/b";
            }
            else if (qualityPreset.Contains("720p", StringComparison.OrdinalIgnoreCase))
            {
                formatSelector = "bv*[height<=720]+ba/b";
            }

            var videoResult = await _ytdl.RunVideoDownload(
                url,
                format: formatSelector,
                mergeFormat: DownloadMergeFormat.Mp4,
                ct: ct,
                progress: progress,
                output: outputLog);

            return videoResult.Success ? videoResult.Data : null;
        }
    }
}
