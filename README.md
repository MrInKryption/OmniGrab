# OmniGrab - Universal Video & Audio Downloader

A 100% native Windows desktop application built with **C# (.NET 8)**, **WPF**, and **Windows 11 Fluent UI (Mica)** that downloads videos from **YouTube, TikTok, Twitch, Reddit, X/Twitter, Instagram, Vimeo**, and 1,800+ other platforms without splitting video and audio tracks.

---

## Key Features

- **Guaranteed Stream Merging**: High-resolution video and audio are automatically multiplexed into a single `.mp4` file using FFmpeg. You never deal with separated video and audio files.
- **Genuine Windows 11 Fluent Aesthetics**: Uses Mica translucency, dark mode, smooth transitions, and native Windows typography via `WPF-UI`.
- **Taskbar Progress Integration**: Displays a live progress bar directly on the Windows Taskbar icon while downloading.
- **Clipboard Quick-Paste**: One-click clipboard intake with instant link inspection.
- **Video Metadata Preview**: Displays video title, uploader, duration, source platform badge, and high-resolution thumbnail before downloading.
- **Flexible Quality Presets**:
  - 🌟 **Best Available Quality** (Auto-merged MP4)
  - 🎬 **1080p Full HD** (Auto-merged MP4)
  - 📺 **720p HD** (Auto-merged MP4)
  - 🎵 **Audio Only - MP3** (320kbps High Quality)
  - 🎧 **Audio Only - M4A / AAC**
- **Session History & Direct Actions**: Download list with one-click **"Play"** (using your default media player) and **"Show in Folder"** (reveals file in Windows Explorer).
- **Core Engine Manager**: Built-in downloader and updater for `yt-dlp.exe` and `ffmpeg.exe` stored in AppData.

---

## How to Run

You can launch the compiled application immediately:
1. Double-click `run.bat` in this folder, or
2. Run `publish\OmniGrab.exe`.

### To Run or Rebuild via .NET CLI:
```powershell
dotnet run
```

To publish a fresh standalone Release build:
```powershell
dotnet publish -c Release -o ./publish
```

---

## Architecture

- **UI Framework**: Windows Presentation Foundation (WPF) with `Wpf.Ui` 4.3 (Windows 11 Fluent controls).
- **Extraction Core**: `yt-dlp` (official latest Windows x64 binary).
- **Audio/Video Muxer**: `FFmpeg` (master build with full codec support).
- **Wrapper**: `YoutubeDLSharp` (async event hooks, progress reporting, cancellation support).
- **Storage Location**: By default, saves to your Windows `Downloads` folder, with custom folder selection.
