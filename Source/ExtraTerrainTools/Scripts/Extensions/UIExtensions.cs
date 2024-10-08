using UnityEngine;
using UnityEngine.UIElements;

namespace TerrainTools
{
    internal static class UIExtensions
    {
         public static void Update(this Slider s, Label l )
        {
            l.text = s.value.ToString();
        }

        public static void UpdateAsInt(this Slider s, Label l )
        {
            if(s.value % 1 > 0) 
                s.value = Mathf.Round(s.value);
            l.text = s.value.ToString();
        }

        public static void UpdateAsPercent(this Slider s, Label l )
        {
            l.text = string.Format("{0} %", Mathf.Round(s.value * 100 ));
        }
    }
}