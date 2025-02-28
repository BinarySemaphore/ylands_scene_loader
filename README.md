# Ylands Scene Loader (Godot Project)
Example loader for exported Ylands scenes

Project is duplicated for both *C#* and *Native* Godot.

Supports most basic cube-shapes, slopes, corners, and spikes along with trusty musket ball<br/>
Supports windows of various sizes as an example for using custom OBJ files
> Note: more will be added

## Requirements
* Godot (version 4.3 native or C#/.NET): [Windows](https://godotengine.org/download/windows/) or [Linux](https://godotengine.org/download/linux/) (*Android* and *MacOS* also supported)

## Controls
* Move: `W`, `A`, `S`, `D`, `Q`, `Z`
* Turn: `Arrow Keys`
* Speed Modifier: `Shift` or `Ctrl`

## Change Scene and Other Options
1. Select the `YlandScene` node
1. In Inspector tab, find `Metadata`
1. Enter path for another scene JSON file for `Scene File`
  > Tip: In Godot you can right click a file in `Filesystem` and `Copy Path`

* Other Options in `YlandScene` node
  * `Box Draw Unsupported`:
    * Toggle on/off (default: on) to draw boxes using blockdef bounding box dimensions
  * `Unsupported Transparency`:
    * if `Box Draw Unsupported` on sets transparency of boxes
    * 0.0 to 1.0 (default 0.5); 0 being invisible, 1 being opaque
  * `Mesh Combine Similar`:
    * Entities within same group will be combined into single mesh separated by surface material; new mesh if max surfaces is exceeded
    * Increased loading time
    * Improved runtime and reduced RAM at load and runtime (large scenes possible on mobile devices)
    * Allows for saving *remote* scene when running, then exporting as `*.gltf` model for import with 3d editors

## Related Ylands Export Project
[Ylands Export Project](https://github.com/BinarySemaphore/ylands_exporter) in GitHub
