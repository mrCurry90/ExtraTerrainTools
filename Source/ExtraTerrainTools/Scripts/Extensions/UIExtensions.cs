using UnityEngine;
using UnityEngine.UIElements;

namespace TerrainTools
{
    internal static class UIExtensions
    {
        #region Slider Extensions
        public static void Update(this Slider s, Label l)
        {
            l.text = s.value.ToString();
        }

        public static void UpdateAsInt(this Slider s, Label l)
        {
            if (s.value % 1 != 0)
                s.value = Mathf.Round(s.value);
            l.text = s.value.ToString();
        }

        public static void UpdateAsPercent(this Slider s, Label l)
        {
            l.text = string.Format("{0} %", Mathf.Round(s.value * 100));
        }
        #endregion

        #region Padding and Margins
        public static void SetPadding(this VisualElement element, float value, LengthUnit unit = LengthUnit.Pixel)
        {
            element.style.paddingTop =
            element.style.paddingRight =
            element.style.paddingBottom =
            element.style.paddingLeft = new Length(value, unit);
        }
        public static void SetPadding(this VisualElement element, float valueTopBottom, float valueLeftRight, LengthUnit unit = LengthUnit.Pixel)
        {
            element.style.paddingTop =
            element.style.paddingBottom = new Length(valueTopBottom, unit);
            element.style.paddingRight =

            element.style.paddingLeft = new Length(valueLeftRight, unit);
        }
        public static void SetPadding(this VisualElement element, float valueTop, float valueLeftRight, float valueBottom, LengthUnit unit = LengthUnit.Pixel)
        {
            element.style.paddingTop = new Length(valueTop, unit);
            element.style.paddingBottom = new Length(valueBottom, unit);
            element.style.paddingRight =
            element.style.paddingLeft = new Length(valueLeftRight, unit);
        }
        public static void SetPadding(this VisualElement element, float valueTop, float valueRight, float valueBottom, float valueLeft, LengthUnit unit = LengthUnit.Pixel)
        {
            element.style.paddingTop = new Length(valueTop, unit);
            element.style.paddingRight = new Length(valueRight, unit);
            element.style.paddingBottom = new Length(valueBottom, unit);
            element.style.paddingLeft = new Length(valueLeft, unit);
        }
        public static void SetMargin(this VisualElement element, float value, LengthUnit unit = LengthUnit.Pixel)
        {
            element.style.marginTop =
            element.style.marginRight =
            element.style.marginBottom =
            element.style.marginLeft = new Length(value, unit);
        }
        public static void SetMargin(this VisualElement element, float valueTopBottom, float valueLeftRight, LengthUnit unit = LengthUnit.Pixel)
        {
            element.style.marginTop =
            element.style.marginBottom = new Length(valueTopBottom, unit);
            element.style.marginRight =

            element.style.marginLeft = new Length(valueLeftRight, unit);
        }
        public static void SetMargin(this VisualElement element, float valueTop, float valueLeftRight, float valueBottom, LengthUnit unit = LengthUnit.Pixel)
        {
            element.style.marginTop = new Length(valueTop, unit);
            element.style.marginBottom = new Length(valueBottom, unit);
            element.style.marginRight =
            element.style.marginLeft = new Length(valueLeftRight, unit);
        }
        public static void SetMargin(this VisualElement element, float valueTop, float valueRight, float valueBottom, float valueLeft, LengthUnit unit = LengthUnit.Pixel)
        {
            element.style.marginTop = new Length(valueTop, unit);
            element.style.marginRight = new Length(valueRight, unit);
            element.style.marginBottom = new Length(valueBottom, unit);
            element.style.marginLeft = new Length(valueLeft, unit);
        }
        #endregion
    }
}