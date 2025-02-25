# ylands_scene_loader
Example loader for exported Ylands scenes

Project is duplicated for both *C#* and *Native* Godot.

Supports most basic cube-shapes, slopes, corners, and spikes along with trusty musket ball<br/>
Supports windows of various sizes as an example for using custom OBJ files
> Note: more will be added

## Requirements
* Godot (version 4.3 native or C#/.NET)
  * [Windows](https://godotengine.org/download/windows/)
  * [Linux](https://godotengine.org/download/linux/)

## Controls
* Move: `W`, `A`, `S`, `D`, `Q`, `Z`
* Turn: `Arrow Keys`
* Speed Modifier: `Shift` or `Ctrl`

## Change Scene
1. Select the `YlandScene` node
1. In Inspector tab, find `Metadata`
1. Enter path for another scene JSON file for `Scene File`
  > Tip: In Godot you can right click a file in `Filesystem` and `Copy Path`

## Related Ylands Export Project
[Ylands Export Project](https://github.com/BinarySemaphore/ylands_exporter)
