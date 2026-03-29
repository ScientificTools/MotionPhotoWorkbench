# First Steps

This page walks through a simple first test with the sample motion photo included in the repository.

## 1. Launch the Application

Start `MotionPhotoWorkbench.exe`.

If `ffmpeg.exe` is missing, the application will show a message explaining that you need to provide it first. See [Installation](Installation.md).

## 2. Load the Sample Motion Photo

Open the `.jpg` sample file provided in [`Samples/`](../../Samples).

Before loading starts, the application may display a warning about the estimated disk space required for the working directory. You must confirm the load before frame extraction begins.

## 3. Review the Main Interface

After loading:

- the frame list appears on the left
- the selected frame preview appears in the center
- the main commands and adjustment controls appear on the right

## 4. Discard Unwanted Frames

You can mark frames that should not be used in the final result.

Discarded frames appear in red in the frame list.

## 5. Place Anchor Points

Click on the preview image to place an anchor point for the selected frame.

Choose a point that can be identified on every frame. This point is used to realign the sequence before export.

For more precise anchor placement, use zoom:

- the `Zoom in` and `Zoom out` buttons
- the mouse wheel on the preview image

## 6. Adjust the Image

Color and detail adjustments are edited from the preview image and then applied consistently to all active frames.

This is a single shared adjustment set for the whole project, not a separate setting per frame.

## 7. Open Preview / Export

Use `Preview / export` to open the export preview window.

This window shows the resulting frame area after all individual frame repositioning. The preview is built from a merged image so movement is easier to see.

From there, you can:

- inspect the automatically computed crop
- crop manually if needed
- use common aspect ratio presets
- choose a free manual crop

Before exporting, you can also choose the target FPS value.

## 8. Export the Result

Four export formats are available:

- MP4
- GIF
- WebM
- animated WebP

These formats are suitable for web use. Example outputs are available in [`Samples/`](../../Samples).

When you are done exporting, close the preview / crop window.

## 9. Save the Project

You can save the current work as a lightweight `.json` project file.

The project file stores the working settings so you can reopen the project later instead of starting from scratch.

In the current application behavior, the saved project includes the source path, frame selection state, anchor points, image adjustments, export FPS, and the current output crop state.

## 10. Clean the Project Cache

If the export result is satisfactory and the project has been saved, you can remove the project cache:

- with the `Clean project cache` button
- or manually in Windows Explorer

This deletes the `_work` directory only.

If you reload the source image or reload the project later, the working directory will be rebuilt automatically when needed.
