# Release Title

`MotionPhotoWorkbench vX.Y.Z`

## What Changed

- Summarize the user-visible improvements.
- Mention important bug fixes.
- Mention any behavior changes that may affect existing users.

## Download

- Windows ZIP: `MotionPhotoWorkbench-vX.Y.Z-win-x64.zip`
- SHA256: `MotionPhotoWorkbench-vX.Y.Z-win-x64.zip.sha256`

## Before You Run

The Windows ZIP is self-contained and does not require a separate .NET runtime install.

`ffmpeg.exe` is required at runtime but is not included in the ZIP.

Setup:

1. Download and extract the Windows ZIP.
2. Download `ffmpeg.exe` from a trusted source.
3. Copy `ffmpeg.exe` next to `MotionPhotoWorkbench.exe`.
4. Start the application.

Recommended FFmpeg sources:

- https://ffmpeg.org/download.html
- https://www.gyan.dev/ffmpeg/builds/

## Traceability

- Git tag: `vX.Y.Z`
- Source commit: `<commit-sha>`
- Built by GitHub Actions from this repository

## Notes

- FFmpeg is a separate third-party dependency and is not redistributed by this project.
- See `THIRD_PARTY_NOTICES.md` in the ZIP and in the repository for dependency details.
