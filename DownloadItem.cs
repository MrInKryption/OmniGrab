using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OmniGrab;

public enum DownloadStatus
{
    Idle,
    FetchingInfo,
    ReadyToDownload,
    Downloading,
    Merging,
    Completed,
    Failed,
    Cancelled
}

public class DownloadItem : INotifyPropertyChanged
{
    public Guid Id { get; } = Guid.NewGuid();

    private string _url = string.Empty;
    public string Url
    {
        get => _url;
        set => SetField(ref _url, value);
    }

    private string _title = "Loading title...";
    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    private string _thumbnailUrl = string.Empty;
    public string ThumbnailUrl
    {
        get => _thumbnailUrl;
        set => SetField(ref _thumbnailUrl, value);
    }

    private string _duration = "--:--";
    public string Duration
    {
        get => _duration;
        set => SetField(ref _duration, value);
    }

    private string _uploader = "Unknown";
    public string Uploader
    {
        get => _uploader;
        set => SetField(ref _uploader, value);
    }

    private string _platform = "Web Video";
    public string Platform
    {
        get => _platform;
        set => SetField(ref _platform, value);
    }

    private string _quality = "Best Quality (MP4)";
    public string Quality
    {
        get => _quality;
        set => SetField(ref _quality, value);
    }

    private string _destinationFile = string.Empty;
    public string DestinationFile
    {
        get => _destinationFile;
        set => SetField(ref _destinationFile, value);
    }

    private double _progressPercentage;
    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => SetField(ref _progressPercentage, value);
    }

    private string _speed = "-- MB/s";
    public string Speed
    {
        get => _speed;
        set => SetField(ref _speed, value);
    }

    private string _eta = "--";
    public string ETA
    {
        get => _eta;
        set => SetField(ref _eta, value);
    }

    private DownloadStatus _status = DownloadStatus.Idle;
    public DownloadStatus Status
    {
        get => _status;
        set
        {
            if (SetField(ref _status, value))
            {
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(StatusBadgeColor));
            }
        }
    }

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    private string _fileSize = "-- MB";
    public string FileSize
    {
        get => _fileSize;
        set => SetField(ref _fileSize, value);
    }

    public DateTime CreatedAt { get; } = DateTime.Now;

    public bool IsActive => Status == DownloadStatus.Downloading || Status == DownloadStatus.Merging || Status == DownloadStatus.FetchingInfo;
    public bool IsCompleted => Status == DownloadStatus.Completed;

    public string StatusBadgeColor => Status switch
    {
        DownloadStatus.Completed => "#2ECC71",
        DownloadStatus.Downloading => "#0078D4",
        DownloadStatus.Merging => "#9B59B6",
        DownloadStatus.Failed => "#E74C3C",
        DownloadStatus.Cancelled => "#95A5A6",
        _ => "#5D6D7E"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
