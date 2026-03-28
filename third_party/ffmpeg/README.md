# FFmpeg Setup

This directory documents how to provide FFmpeg manually for local use.

FFmpeg is required by MotionPhotoWorkbench for frame extraction and video export, but it is not redistributed in this repository.

Recommended download sources:

- Official FFmpeg download page: https://ffmpeg.org/download.html
- Recommended Windows build provider: https://www.gyan.dev/ffmpeg/builds/

Recommended Windows choice:

- Windows x64 build
- `release essentials` variant

Manual setup:

1. Download a trusted Windows x64 FFmpeg build.
2. Extract the archive.
3. Copy `ffmpeg.exe` next to the compiled application executable.

Typical output location:

```text
bin\Debug\net8.0-windows\
bin\Release\net8.0-windows\
```

The application expects `ffmpeg.exe` beside the executable.
