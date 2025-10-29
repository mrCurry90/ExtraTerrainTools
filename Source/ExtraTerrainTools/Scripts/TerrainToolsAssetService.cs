using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Timberborn.AssetSystem;
using Timberborn.SingletonSystem;

namespace TerrainTools
{
    public class TerrainToolsAssetService : ILoadableSingleton
    {
        public enum Folder
        {
            None,
            Materials,
            Prefabs,
            Textures,
            Sounds,
            Localizations,
            Specifications,
            Sprites,
            UI
        }

        private readonly Dictionary<Folder, string> paths = new();
        private readonly IAssetLoader _assetLoader;
        private readonly static string materialPath = "Materials";
        private readonly static string prefabsPath = "Prefabs";
        private readonly static string texturesPath = "Textures";
        private readonly static string soundsPath = "Sounds";
        private readonly static string locPath = "Localizations";
        private readonly static string specPath = "Specifications";
        private readonly static string spritePath = "Sprites";
        private readonly static string uiPath = "UI";

        public TerrainToolsAssetService(IAssetLoader assetLoader)
        {
            _assetLoader = assetLoader;
            paths.Add(Folder.None, "");
            paths.Add(Folder.Materials, materialPath);
            paths.Add(Folder.Prefabs, prefabsPath);
            paths.Add(Folder.Textures, texturesPath);
            paths.Add(Folder.Sounds, soundsPath);
            paths.Add(Folder.Localizations, locPath);
            paths.Add(Folder.Specifications, specPath);
            paths.Add(Folder.Sprites, spritePath);
            paths.Add(Folder.UI, uiPath);
        }

        public void Load()
        {
        }

        private string GetFolderPath(Folder folder)
        {
            if (paths.TryGetValue(folder, out string value))
                return value;
            else throw new KeyNotFoundException(folder.ToString());
        }

        public T Fetch<T>(string path, Folder folder = Folder.None)
        where T : UnityEngine.Object
        {
            string assetPath = Path.Combine(GetFolderPath(folder), path);
            return _assetLoader.Load<T>(assetPath);
        }

        public IEnumerable<T> FetchAll<T>(string path, Folder folder = Folder.None)
        where T : UnityEngine.Object
        {
            string assetPath = Path.Combine(GetFolderPath(folder), path);
            return _assetLoader.LoadAll<T>(assetPath).Select((l) => l.Asset);
        }
    }
}