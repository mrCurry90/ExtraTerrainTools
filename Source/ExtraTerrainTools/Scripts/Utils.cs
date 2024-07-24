using UnityEngine.UIElements;
using Timberborn.AssetSystem;
using System.Reflection;
using System.Linq;
using UnityEditor;

namespace TerrainTools {
    internal class Utils
    {
        private static readonly IAssetLoader _assetLoader;

        public static void Log( string text ) {
            UnityEngine.Debug.Log( "Mod: TerrainTools - " + text );
        }

        public static void Log( string text, params object[] args)
        {
            Log(string.Format(text, args));
        }

        public static void LogVisualTree(VisualElement node)
        {
            LogVisualTree(node, 0);
        }

        private static void LogVisualTree( VisualElement node, int level)
        {
            string indent = "", tab = "	";
            for (int i = 0; i < level; i++)
                indent += tab;

            Log( indent + node.GetType() + " - " + node.name );
            foreach (var c in node.Children())
                LogVisualTree( c, level + 1 );
        }

        public static void LogAssetsInPath<T>(IAssetLoader _assetLoader, string path) where T : UnityEngine.Object
        {
            var assets = _assetLoader.LoadAll<T>(path);
            Log("Assets @ " + path + ": " + assets.Count());
            foreach (var a in assets)
                Log(a.Asset.name + ": " + a.Asset.GetType() + " - Path: " + path + "/" + a.Asset.name);
        }
    }
}