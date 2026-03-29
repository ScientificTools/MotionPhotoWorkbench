# Installation

MotionPhotoWorkbench currently targets Windows.

## Option 1: Use a Release Build

This is the easiest option for most users.

1. Download the latest Windows self-contained ZIP from GitHub `Releases`.
2. Extract the ZIP to a local folder.
3. Download `ffmpeg.exe` separately.
4. Copy `ffmpeg.exe` next to `MotionPhotoWorkbench.exe`.
5. Start `MotionPhotoWorkbench.exe`.

Recommended FFmpeg sources:

- https://ffmpeg.org/download.html
- https://www.gyan.dev/ffmpeg/builds/

Current note about Windows warnings:

- The executable is not signed yet, so Windows may show a warning the first time you launch it.
- The release package is generated automatically by GitHub Actions from the repository sources used for that tagged release.
- A published checksum is provided so the downloaded archive can be verified locally.
- Code signing is planned for a future release.

## Option 2: Build from Source

Use this option if you want to work from the repository source code.

1. Clone this repository.
2. Open the solution in Visual Studio Community on Windows.
3. Build the solution.
4. Download `ffmpeg.exe` separately.
5. Copy `ffmpeg.exe` next to the built executable.
6. Start the application.

Typical build output locations:

```text
bin\Debug\net8.0-windows\
bin\Release\net8.0-windows\
```

FFmpeg is required for frame extraction and video export, but it is not bundled with this repository.

