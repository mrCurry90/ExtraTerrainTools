using UnityEngine.UIElements;
using Timberborn.AssetSystem;
using System.Linq;
using System.Text;

namespace TerrainTools
{
    internal class Utils
    {
        private static string _PREFIX = "Mod: TerrainTools - ";

        public static void Log(string text)
        {
            UnityEngine.Debug.Log(_PREFIX + text);
        }

        public static void Log(string text, params object[] args)
        {
            Log(string.Format(text, args));
        }

        public static void LogError(string text)
        {
            UnityEngine.Debug.LogError(_PREFIX + text);
        }

        public static void LogError(string text, params object[] args)
        {
            LogError(string.Format(text, args));
        }

        public static void LogIndented(int indents, string text, params object[] args)
        {
            StringBuilder indentedString = new StringBuilder();
            indentedString.Append("\t".PadLeft(indents));
            indentedString.Append(text);

            Log(string.Format(indentedString.ToString(), args));
        }

        public static void LogVisualTree(VisualElement node)
        {
            LogVisualTree(node, 0);
        }

        private static void LogVisualTree(VisualElement node, int level, int siblingIndex = 0)
        {
            string indent = "", tab = "	";
            for (int i = 0; i < level; i++)
                indent += tab;

            Log(indent + node.GetType() + " - " + node.name + " - Index " + siblingIndex);

            siblingIndex = 0;
            foreach (var c in node.Children())
            {
                LogVisualTree(c, level + 1, siblingIndex);
                siblingIndex++;
            }
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