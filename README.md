![TerrainToolsLogo](https://github.com/user-attachments/assets/18d32edf-9f32-4934-9c68-e7e44abea8fe)

**Description**

I love making maps with a natural feel but building up "nature" from the blank canvas provided by Timberborn's map editor can be a daunting task.
This mod aims to help with that by providing tools to budding and experienced map creators alike.
The goal is to build those tools based on well-established techniques in procedural generation to allow you to quickly create and iterate on your map design.

Right now the "suite" is kinda small with just one finished tool, but I have a few ideas in the pipe.
If there are any specific creative tools you would like see in the editor then leave a comment or start an Request-issue on GitHub.

@Modders: Included in the suite is a simple way to add more tools to the Extra Terrain Tools-toolbar.
If you wish add your own tools to the toolbar check out NoiseGeneratorConfigurator.cs for the gist of how to set it up.

**Tools in current release**

- Heightmap Generator - Generate a procedurally generated map with a single click. See the Heightmap Generator Manual for more.

**Planned tools**

- Mountain Maker
- Riverbed Runner
- More?

**Known issues**

- Changing large chunks of terrain while water is on the map can cause issues with the game's water system. The Heightmap Generator does its best to clear up the watersources and existing water before applying changes but occasionally this is not enough.
**Recommendation:** if you have placed water/badwater sources **Save before clicking Generate**.


