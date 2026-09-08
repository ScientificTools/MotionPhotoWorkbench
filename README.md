# MotionPhotoWorkbench

**What does this tool do?**  
It is designed to reprocess a short video or a motion photo (from a smartphone or a camera that captures burst sequences of 20 to 80 images) by applying image-by-image processing before rebuilding a video in a standard, lightweight export format suitable for web pages (WebM, WebP, MP4). You can choose which frames to keep, stabilize the animation with an automatic anchor-point system, apply color adjustments to the whole sequence, crop the result to a rectangle with the aspect ratio you want, set the playback speed in frames per second, preview the result, and then export it to a standard, lightweight format that can be used directly in a web page (WebM, MP4, WebP, animated GIF) or in a video player (WebM, MP4).

**There are already tools available for this. What does MotionPhotoWorkbench bring to the table?**

MotionPhotoWorkbench is **not** useful in the following cases:

* If you are just looking to convert your video to WebM:
  * You can directly use the FFmpeg command line (MotionPhotoWorkbench uses it under the hood anyway): `ffmpeg -i file.mp4 -c:v libvpx-vp9 -b:v 0 -crf 30 -c:a libopus file.webm`
* If it's for occasional use, you can do it online at cloudconvert.com


* If you shoot motion photos with your smartphone, some devices offer automatic stabilization:
  * On a Google Pixel, from the Photos app, go to `...` > **Save as...** > **Video**: your stabilized video will be exported to your gallery in MPEG format, right next to the motion photo, appended with "-stabilized".
  * If the result works for you, you're all set—no need to go any further.
  * Since stabilization is automatic, it doesn't work in every situation (results can be hit-or-miss with digiscoping or spotting scopes), and certain frames from the burst are automatically discarded, which can be frustrating. However, it often turns out well and usable.



I designed MotionPhotoWorkbench for all other cases that lacked a seamless, all-in-one solution:

* **Full control over frame selection:** Easily pick which images to keep in the sequence using the `Delete` or `Enter` keys, or dedicated buttons.
* **Precise stabilization control:**
  * Select a tracking point on the screen on the first image, then launch the search across the following frames.
  * It memorizes a small bounding box around this point and searches for it on the next image using a spiral search pattern.
  * It keeps the closest matching point and moves on to the next frame.
  * Two customizable confidence thresholds: Below 85%, it gives up. Above 95%, it confirms the match (green). Between the two, it keeps the match but flags it with a warning (orange).
  * You can visually see the detected points on every frame.
  * You can fine-tune or reposition points manually, restart tracking for subsequent frames, or manually set the point on every single frame.


* **Easy video cropping after frame alignment:**
  * All frames are overlaid with transparency, making it easy to spot camera movement and select an appropriate zoom rectangle in your desired aspect ratio.


* **Homogeneous color grading:**
  * Apply consistent color adjustments across all images—useful if a video lacks pop.


* **Seamless looping (Yoyo effect):**
  * Short web videos played on a loop often suffer from a jarring jump when restarting after 2 to 4 seconds. Enabling the **yoyo effect** generates a video that plays forward then backward, creating a smooth, invisible transition between the last and first frames *(note: not suitable for subjects moving continuously in one direction)*.

**Example:** source motion image from a Pixel 9a, captured through a spotting scope, **with strong instability** — 3.5 MB

[rougeGorge_source.webm](https://github.com/user-attachments/assets/6a9e5c65-248a-49af-a273-41ece9fb6861)

**Result below:**  
Processed video in **WEBM** format, **0.3 MB**, with **automatic frame recentering**, **color correction**, and **export-area selection in a 4:3 aspect ratio**. This processing was performed with every other frame kept.

[RougeGorge__stabilise_roundTrip_1ImageSur2_colorimetrie.webm](https://github.com/user-attachments/assets/974edf00-6170-40c1-be94-c6998ea763f0)


**What is a motion photo?**  
Simply put: with a smartphone (or a suitable camera), you take a photo as usual. The device has already recorded 1 second of video before you press the shutter, and then it continues recording briefly afterward.

**What is it for? What does it add?**  
You may ask that question... When this image-video file comes out of the smartphone, it is a `.JPG` file that contains, through a proprietary encoding trick, a still photo and an MPEG video appended at the end of the file.

Very few programs know how to use the embedded video at the end of the file. Most can only display the still image. Web pages also cannot normally make use of it except through special handling.

So what is it for? Not much, as is... You are effectively limited to viewing the animation on your smartphone or camera. It can still be useful for choosing the best shot from the burst sequence. And yet, these short animations bring life and depth to a photo.

For bird photography, where the subject is constantly moving, 1 second of animation is already a treat — a small slice of life compared with a static photo.

**What are the difficulties?**  
In bird photography, images are often taken through a spotting scope, using an inexpensive smartphone (digiscoping), or with a camera fitted with a powerful telephoto lens (400 mm, 600 mm, 800 mm). With that much magnification, the slightest movement becomes obvious.

As the owner of a stabilized spotting scope (mainly because I was too lazy to carry a tripod) and a smartphone for digiscoping, I tried motion photos and, faced with the lack of suitable software, I decided to create this tool.

MotionPhotoWorkbench was born from limited means: I do not have a Mac, I work on Windows, so for now it targets Windows only. If this project attracts enough interest from a community, it may evolve further.

**How it works**

This program relies on **FFmpeg** https://github.com/ffmpeg/ffmpeg (free to use, widely adopted, and efficient — thanks to its authors) to **split the video into individual frames** and **rebuild** the sequence afterward.

The program provides controls to **automatically recenter each frame around an anchor point** (stabilization) and **automatically align and crop the frames around that anchor point**. You can also apply **color correction**.

**What image sources can it process?**  
Several different sources:

- a directory containing already extracted, ordered photos (whatever their naming convention or format, they will be taken in alphabetical order): here we skipped the video stage and started directly from image files;
- standard video files in most common formats — but please keep them short. With 2 seconds, you will already need roughly 150 MB of disk space for processing, so for 1 minute... 4.5 GB;
- motion images produced by smartphones such as Google Pixel, Samsung, or iPhone, with their proprietary formats: a still image plus a video appended in the same file.

**Note:** given my limited smartphone budget, I used the one I had, a Google Pixel 9a, which conveniently has only two lenses — very handy for digiscoping.

**File size**

- The **0.3 MB** WebM version is the best option: eco-friendly and surprisingly small compared with a single image.
- The **source motion image** from the Pixel 9a: **3.5 MB**
- The **raw MPEG extracted** by MotionPhotoWorkbench: **2.3 MB** and contains **45 frames**
- An equivalent **WebM** (same number and size of frames): **0.5 MB**
- During processing, individual frames are handled as PNG files. Here, **each PNG is about 1 MB**
- The workflow requires 3 temporary directories:
  - `frames`: the 45 original frames as PNG
  - `final`: the working frames, viewed with color correction; discarded frames are not included. Each color-correction change starts again from the original `frames` directory: no image quality loss.
  - `aligned`: the centered and cropped images, ready to be merged into a video. 45 images here.
- The **temporary directory size** is therefore about **140 MB** in this case: 1 MB (PNG) × 45 (images) × 3 (directories) + 2.3 MB of MPEG video extracted from the motion image. This temporary directory is only a working area and can be deleted once processing is complete.

**Image quality**

There is no magic: a lightly compressed still photo will always deliver more sharpness and detail, with that wow effect and the possibility of printing it in a large format.  
But for viewing on screen, a motion photo adds that extra sense of life and depth: its purpose is not to be printed, but to capture a moment and a behavior, much like a field note.

Graphic photographs and documentary videos each have their own interest and audience. Motion photos sit between the two — neither still photo nor video, but both at once. Their strength is that they are short and lively.

In terms of size versus quality, the WebM format is impressive — almost eco-friendly. My results usually range between 300 KB and 1 MB, rarely more, and average around 500 KB, so there is no shame in showing them online.

For more examples of results, you can visit my birdwatching and urban nature walking site: https://www.baladechampvert.fr

## Code License

The code is licensed under the MIT License, so it may be freely reused and adapted. See [LICENSE.txt](LICENSE.txt).
