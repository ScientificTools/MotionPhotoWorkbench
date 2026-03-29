# Get Started

MotionPhotoWorkbench is a Windows desktop tool for turning a motion photo or a short video into a cleaned-up exported animation.

With the application, you can:

- extract frames from a motion photo or a short video
- realign frames when unwanted camera or subject movement affects the sequence
- discard frames that should not be kept in the final result
- apply the same color and detail adjustments to all frames
- preview the maximum usable crop after alignment
- refine that crop manually if needed
- export the final result as MP4 (MPEG-4), GIF, WebM, or animated WebP

You can also save your work to a project file and reload it later to continue from the same settings.

## Working Directory Warning

When you load a motion photo or reload a project, the application expands all frames into a working directory named `_work`.

Important notes:

- `_work` can become large very quickly
- a 5 MB motion photo representing about 1 to 2 seconds of video can require roughly 200 MB of disk space in `_work`
- before loading, the application can show an approximate disk space warning when the expected working directory is large

If you no longer need the cache, you can remove `_work` in two ways:

- delete the `_work` directory manually in Windows Explorer
- use the `Clean project cache` button in the application

`Clean project cache` deletes the `_work` directory only. It does not delete the project file.

## Sample Files

The [`Samples/`](../../Samples) folder contains:

- an example motion photo
- generated output files created by the application

These files are useful for a quick first test of the workflow and export formats.
