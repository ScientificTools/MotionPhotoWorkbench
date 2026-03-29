# MotionPhotoWorkbench

Desktop tool for extracting frames from a motion photo or video, choosing anchor points, aligning the kept frames, adjusting the image, and exporting the result as GIF, MP4, WebM, or animated WebP.

This repository is currently focused on Windows and WinForms. The codebase is published as source code for learning, inspection, and contribution, without a support commitment.

## Features

- Open a motion photo compatible JPEG, a regular image, or a video file
- Extract frames through FFmpeg
- Select kept or discarded frames
- Place an anchor point per frame and align the sequence
- Adjust brightness, contrast, saturation, temperature, sharpness, highlights, and shadows
- Preview the automatic crop and refine it before export
- Export to GIF, MP4, WebM, or animated WebP
- Save and reload project state as JSON

## Current Status

- Target framework: `.NET 8`
- UI technology: `WinForms`
- Supported OS for the current app: `Windows`
- Repository status: source-first, no installer provided
- Recommended distribution model for non-technical users: signed or checksummed ZIP builds published in GitHub `Releases`

## Requirements

- Windows
- .NET 8 SDK
- FFmpeg available next to the compiled executable as `ffmpeg.exe`

## For End Users

If you do not want to install Visual Studio or the .NET SDK, use a packaged build from the repository `Releases` page when one is available.

Recommended end-user flow:

1. Download the latest Windows self-contained ZIP asset from `Releases`.
2. Extract it to a local folder.
3. Download `ffmpeg.exe` separately from a trusted source.
4. Copy `ffmpeg.exe` next to `MotionPhotoWorkbench.exe`.
5. Launch the application.

This repository intentionally does not bundle FFmpeg. See `THIRD_PARTY_NOTICES.md` and `third_party/ffmpeg/README.md` for the setup details.

## Build

```powershell
dotnet restore
dotnet build MotionPhotoWorkbench.sln -c Release
```

The project file currently targets `net8.0-windows`, so the application is expected to build and run on Windows.

## Run

Build the solution, then place `ffmpeg.exe` next to the generated executable before launching the application.

Typical output location:

```text
bin\Debug\net8.0-windows\
bin\Release\net8.0-windows\
```

## Public Release Guidance

For a public repository, it is acceptable and common to publish ready-to-run binaries for non-technical users, provided that the distribution remains transparent and traceable.

Recommended good practices for this repository:

- Publish binaries through GitHub `Releases`, not as committed `.exe` files inside the repository history.
- Tie each binary release to a Git tag such as `v1.2.3`.
- Make sure the released ZIP is built from the exact tagged source.
- Include a short changelog and the source tag or commit in the release notes.
- Publish a `SHA256` checksum for the ZIP so users can verify integrity.
- State clearly that `ffmpeg.exe` is required at runtime but is not included.
- Keep license and third-party notices easy to find from the README and release notes.
- If possible later, add Windows code signing for additional trust.

This repository includes a GitHub Actions workflow to package a Windows release ZIP and checksum automatically when a version tag is pushed.

Suggested release process:

1. Update the version and documentation.
2. Commit and push to `master`.
3. Create and push a tag such as `v1.2.3`.
4. Let GitHub Actions build the Windows ZIP and checksum.
5. Publish the GitHub Release with the generated artifacts and release notes.

## FFmpeg

FFmpeg is required for frame extraction and video export.

- FFmpeg is not redistributed in this repository
- Official FFmpeg download page: `https://ffmpeg.org/download.html`
- Recommended Windows provider page: `https://www.gyan.dev/ffmpeg/builds/`
- Recommended Windows choice: x64 `release essentials`
- After download, place `ffmpeg.exe` beside the application executable
- See `THIRD_PARTY_NOTICES.md` and `third_party/ffmpeg/README.md` for setup guidance

Important distribution note:

- The application ZIP published in GitHub `Releases` should not include FFmpeg unless you explicitly decide to take on that redistribution and license-review responsibility.

## Samples

The repository includes real sample media in `Samples/` so that visitors can quickly test the software and compare the exported formats.

Included samples:

- a motion photo JPEG source
- exported GIF, MP4, WebM, and animated WebP files
- a small HTML page for previewing the exported files in a browser

The previously saved `project.json` sample is intentionally not published because it contained personal absolute filesystem paths.

## Known Limitations

- Windows-only UI for now
- No packaged installer
- FFmpeg path is not configurable from the UI yet
- Some motion photo variants may contain embedded video in formats this tool does not detect automatically
- The repository does not ship a reusable sample project JSON because local path data was removed from publication

## Roadmap

- Improve public documentation and onboarding
- Add CI validation for public contributions
- Decouple core processing logic further from the WinForms UI
- Evaluate a future cross-platform UI path if the project direction justifies it

## Contributing

Small improvements and bug reports are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for lightweight contribution guidelines.

## Licensing

The repository uses two licenses:

- Source code is licensed under the MIT License
- Media files and demo assets in [`Samples/`](/c:/perso/VisualStudio/MotionPhotoWorkbench/Samples) are licensed under Creative Commons Attribution 4.0 International (`CC BY 4.0`)

This means the software code and the sample media do not share the same redistribution terms. For the sample media details and required attribution, see [`Samples/LICENSE.md`](/c:/perso/VisualStudio/MotionPhotoWorkbench/Samples/LICENSE.md).

## Code License

This project is licensed under the MIT License. See [LICENSE.txt](LICENSE.txt).

