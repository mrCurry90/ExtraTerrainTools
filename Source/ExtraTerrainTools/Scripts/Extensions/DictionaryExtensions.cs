using System.Collections.Generic;

namespace TerrainTools
{
    public static class DictionaryExtensions
    {
        public static void SetOrAdd<TKey>(this Dictionary<TKey, int> dict, TKey key, int value)
        {
            if (dict.ContainsKey(key))
                dict[key] += value;
            else
                dict[key] = value;
        }

        public static void SetOrAdd<TKey>(this Dictionary<TKey, float> dict, TKey key, float value)
        {
            if (dict.ContainsKey(key))
                dict[key] += value;
            else
                dict[key] = value;
        }

        public static void SetOrAdd<TKey>(this Dictionary<TKey, double> dict, TKey key, double value)
        {
            if (dict.ContainsKey(key))
                dict[key] += value;
            else
                dict[key] = value;
        }
    }
}