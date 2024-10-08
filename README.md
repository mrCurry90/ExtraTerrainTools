![TerrainToolsLogo](https://github.com/user-attachments/assets/18d32edf-9f32-4934-9c68-e7e44abea8fe)

### Description

This is a Timberborn mod.

I love making maps with a natural feel but building up "nature" from the blank canvas provided by Timberborn's map editor can be a daunting task.
This mod aims to help with that by providing tools to budding and experienced map creators alike.
The goal is to build those tools based on well-established techniques in procedural generation to allow you to quickly create and iterate on your map design.

Right now the "suite" is kinda small with just one finished tool, but I have a few ideas in the pipe.
If there are any specific creative tools you would like to see then leave a comment or start an Request-issue on GitHub.

@Modders: Included in the suite is a simple way to add more tools to the Extra Terrain Tools-toolbar.
If you wish add your own tools to the toolbar check out NoiseGeneratorConfigurator.cs for the gist of how to set it up.

### Tools in v0.9

- Editor History - You can now Undo your mistakes using all new shortcuts and keybindings to match.
- - **Undo** / **Redo** - *Ctrl + Z* / *Y*
- - **Undo x 5** / **Redo x 5** - *Ctrl + Shift + Z* / *Y*
- Smoothing Brush - Smooth those harsh spikes and cliffs from the Heightmap Generator.
- Mound Maker - Build a mountain or a boulder field. Using conical geometry as the base and multiple layers of noise to make mountain looking features. 
- - Also comes with a dig mode (make a mountain shaped hole).
- - Tweaking the noise parameters can give you a lot of different and interesting shapes.
- Heightmap Generator - Generate a procedurally generated map with a single click. See the [Heightmap Generator Manual](https://docs.google.com/document/d/1Y35eAUWDHY_j4pUGkCSBjHaMf2RxPHbqJ7wJm86qDFs/edit?usp=drive_link) for more details.
 

### Planned tools

- Line Layer (Make ridges or canals using unity's spline package )

### Required Game version

Update 6

### Installation

Use Steam Workshop or Mod.io

### Known issues

- Changing large chunks of terrain while water is on the map can cause issues with the game's water system. The Heightmap Generator does its best to clear up the watersources and existing water before applying changes but occasionally this is not enough.
**Recommendation:** if you have placed water/badwater sources **Save before clicking Generate**.


