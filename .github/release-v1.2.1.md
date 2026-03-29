# MotionPhotoWorkbench v1.2.1

## What Changed

This release is the first public Windows package intended for non-technical users who want to try the application without building it from source.

User-visible improvements in this version:

- Better first-run guidance when `ffmpeg.exe` is missing, with a clear message explaining where to download it and where to place it.
- A safety check before heavy frame extraction when a source may generate a large `_work` directory.
- An estimated `_work` size warning based on frame count before extraction.
- A warning when available disk space looks insufficient for the expected `_work` directory.
- A `Clean project cache` button to delete the `_work` directory without deleting the JSON project file.
- Clearer recovery flow when `_work` was deleted: the app now warns the user and offers to rebuild the project cache.

## Download

- Windows self-contained ZIP: `MotionPhotoWorkbench-v1.2.1-win-x64.zip`
- SHA256: `MotionPhotoWorkbench-v1.2.1-win-x64.zip.sha256`

## Before You Run

The Windows ZIP is self-contained and does not require a separate .NET runtime install.

`ffmpeg.exe` is required at runtime but is not included in the ZIP.

Setup:

1. Download and extract `MotionPhotoWorkbench-v1.2.1-win-x64.zip`.
2. Download `ffmpeg.exe` from a trusted source.
3. Copy `ffmpeg.exe` next to `MotionPhotoWorkbench.exe`.
4. Start the application.

Recommended FFmpeg sources:

- https://ffmpeg.org/download.html
- https://www.gyan.dev/ffmpeg/builds/

Recommended Windows choice:

- Windows x64
- `release essentials`

## Traceability

- Git tag: `v1.2.1`
- Source repository: this GitHub repository
- Intended build source: tagged source for `v1.2.1`
- Intended packaging: GitHub Actions release workflow

## Notes

- FFmpeg is a separate third-party dependency and is not redistributed by this project.
- See `THIRD_PARTY_NOTICES.md` in the ZIP and in the repository for dependency details.
- If Windows SmartScreen warns on first launch, users should verify they downloaded the ZIP from the official GitHub `Releases` page for this repository and optionally verify the published SHA256 checksum.
