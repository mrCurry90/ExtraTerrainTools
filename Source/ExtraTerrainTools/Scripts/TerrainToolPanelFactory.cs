using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Timberborn.BehaviorSystem;
using Timberborn.CoreUI;
using Timberborn.DropdownSystem;
using Timberborn.SingletonSystem;
using UnityEngine;
using UnityEngine.UIElements;
namespace TerrainTools
{
    public class TerrainToolPanelFactory : ILoadableSingleton
    {
        [Flags]
        public enum TextFormat
        {
            None = 0,
            Bold = 1,
            Italic = 2
        }

        private readonly VisualElementLoader _loader;
        private readonly DropdownItemsSetter _dropdownItemsSetter;
        private static readonly string brushShapePath = "MapEditor/ToolPanel/BrushShapePanel";
        private static readonly string brushSizePath = "MapEditor/ToolPanel/BrushSizePanel";
        private static readonly string thumbnailPath = "MapEditor/ToolPanel/ThumbnailCapturingPanel";
        private static readonly string spawningPath = "MapEditor/ToolPanel/NaturalResourceSpawningBrushPanel";
        private static readonly string togglePath = "MapEditor/ToolPanel/ToolPanelToggle";
        private static readonly string newMapBoxPath = "Options/NewMapBox";
        private static readonly string dropdownPath = "Game/BatchControl/DropdownBatchControlRowItem";

        public TerrainToolPanelFactory(
            VisualElementLoader visualElementLoader,
            DropdownItemsSetter dropdownItemsSetter
        )
        {
            _loader = visualElementLoader;
            _dropdownItemsSetter = dropdownItemsSetter;
        }

        public void Load()
        {

        }

        private VisualElement LoadVisualElement(string path)
        {
            return _loader.LoadVisualElement(path);
        }
        private VisualElement LoadChild(string assetPath, int[] indexPath)
        {
            int i = 0;
            try
            {
                VisualElement parent = LoadVisualElement(assetPath);
                VisualElement child = null;
                foreach (var step in indexPath)
                {
                    child = parent[step];
                    parent = child;
                    i++;
                }

                return child;
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentOutOfRangeException("indexPath", "Invalid child index at position " + i);
            }
        }
        private VisualElement LoadChild(string assetPath, string childName)
        {
            var root = LoadVisualElement(assetPath);
            VisualElement child = root.Q<VisualElement>(childName);
            return child;
        }
        private VisualElement LoadChild<T>(string assetPath) where T : VisualElement
        {
            var root = LoadVisualElement(assetPath);
            VisualElement child = root.Q<T>();
            return child;
        }

        public FlexDirection FlexHorizontal { get; } = FlexDirection.Row;
        public FlexDirection FlexVertical { get; } = FlexDirection.Column;

        public Align AlignStart { get; } = Align.FlexStart;
        public Align AlignCenter { get; } = Align.Center;
        public Align AlignEnd { get; } = Align.FlexEnd;
        public Align AlignStretch { get; } = Align.Stretch;

        public VisualElement MakeTemplatePanel(FlexDirection flexDirection = FlexDirection.Column, Align alignItems = Align.Center, Justify justifyContent = Justify.FlexStart)
        {
            VisualElement template = LoadVisualElement(brushShapePath);
            template.Clear();
            SetElementLayout(template, flexDirection, alignItems, justifyContent);
            return template;
        }

        public VisualElement MakeContainer(FlexDirection flexDirection = FlexDirection.Column, Align alignItems = Align.Center, Justify justifyContent = Justify.FlexStart)
        {
            VisualElement container = new();
            SetElementLayout(container, flexDirection, alignItems, justifyContent);
            return container;
        }

        public void SetElementLayout(VisualElement container, FlexDirection flexDirection, Align alignItems, Justify justifyContent)
        {
            container.style.flexDirection = flexDirection;
            container.style.alignItems = alignItems;
            container.style.justifyContent = justifyContent;
        }

        public Label MakeLabel(string text)
        {
            int[] path = { 0, 0 };
            var label = LoadChild(brushShapePath, path) as Label;
            label.text = text;
            return label;
        }

        public Label MakeLabel(string text, TextAnchor alignAnchor, TextFormat format = TextFormat.None)
        {
            if (format != TextFormat.None)
            {
                if (format.HasFlag(TextFormat.Bold)) text = $"<b>{text}</b>";
                if (format.HasFlag(TextFormat.Italic)) text = $"<i>{text}</i>";
            }

            var label = MakeLabel(text);
            label.style.unityTextAlign = alignAnchor;
            return label;
        }

        private Button MakeButton(string assetPath, string childName, string text, Action action)
        {
            var button = LoadChild(assetPath, childName) as Button;
            button.text = text;
            button.RegisterCallback<ClickEvent>(delegate
            {
                action();
            });
            return button;
        }

        private Button MakeButton<T>(string assetPath, string childName, string text, Action<Button, T> action, T arg)
        {
            var button = LoadChild(assetPath, childName) as Button;
            button.text = text;
            button.RegisterCallback<ClickEvent>(delegate
            {
                action(button, arg);
            });
            return button;
        }

        public Button MakeButton<T>(string text, Action<Button, T> action, T arg)
        {
            return MakeButton(thumbnailPath, "Update", text, action, arg);
        }
        public Button MakeButton(string text, Action action)
        {
            return MakeButton(thumbnailPath, "Update", text, action);
        }
        public Button MakeMinusButton(Action action)
        {
            return MakeButton(brushSizePath, "Minus", "", action);
        }
        public Button MakePlusButton(Action action)
        {
            return MakeButton(brushSizePath, "Plus", "", action);
        }

        public TextField MakeTextField(Action changeAction = null)
        {
            var textField = LoadChild(newMapBoxPath, "SizeXField") as TextField;
            if (changeAction != null)
            {
                textField.RegisterValueChangedCallback(delegate
                {
                    changeAction();
                });
            }

            return textField;
        }
        /// <summary>
        /// Returns a container element for a toggle button.
        /// The actual Toggle can be accessed by querying the container for it using .Q&lt;Toggle&gt;>()
        /// </summary>
        /// <param name="text">Text shown next to toggle</param>
        /// <param name="toggleAction">Action to perform</param>
        /// <param name="defaultState">Starting state, does not execute toggleAction</param>
        /// <returns></returns>
        public Toggle MakeToggle(string text, Action toggleAction, bool defaultState = false)
        {
            Toggle toggle = LoadVisualElement(togglePath).Q<Toggle>();
            toggle.text = text;
            toggle.value = defaultState;
            toggle.RegisterValueChangedCallback(delegate
            {
                toggleAction();
            });

            return toggle;
        }

        public Toggle MakeToggle(string text, Action<Toggle> toggleAction, bool defaultState = false)
        {
            Toggle toggle = LoadVisualElement(togglePath).Q<Toggle>();
            toggle.text = text;
            toggle.value = defaultState;
            toggle.RegisterValueChangedCallback(delegate
            {
                toggleAction(toggle);
            });

            return toggle;
        }

        // public ImageToggle MakeImageToggle(Texture on, Texture off, Action<bool> toggleAction, bool defaultState = false)
        // {
        //     return new ImageToggle(on, off, toggleAction, defaultState);
        // }

        /// <summary>
        /// Returns a container element with a Slider "Slider" and a Label "SliderValue".
        /// Update of the value label is NOT handled automatically.
        /// The Slider and Label be accessed by querying the container for using .Q(string)
        /// </summary>
        /// <param name="label">Label for the slider</param>
        /// <param name="changeAction">OnValueChanged eventhandler</param>
        /// <returns></returns>
        public VisualElement MakeSlider(string label, Action changeAction, float min = 0f, float max = 100f, float? initial = null)
        {
            int[] path = { 1 };
            VisualElement container = LoadChild(spawningPath, path);
            Slider slider = container.Q("Slider") as Slider;
            slider.label = label;
            slider.value = initial.HasValue ? Mathf.Clamp((float)initial, min, max) : min;
            slider.lowValue = min;
            slider.highValue = max;
            slider.RegisterValueChangedCallback(delegate
            {
                changeAction();
            });

            return container;
        }

        public VisualElement MakeDropdown(IDropdownProvider dropdownProvider)
        {
            VisualElement visualElement = _loader.LoadVisualElement(dropdownPath);
            Dropdown dropdown = visualElement.Q<Dropdown>("Dropdown");
            _dropdownItemsSetter.SetItems(dropdown, dropdownProvider);
            // _tooltipRegistrar.Register(dropdown, () => GetTooltipText(dropdownProvider));

            return visualElement;
        }

        public VisualElement MakeDropdown(IExtendedDropdownProvider dropdownProvider, bool reverse = false, bool displayItemText = true)
        {
            VisualElement visualElement = _loader.LoadVisualElement(dropdownPath);
            Dropdown dropdown = visualElement.Q<Dropdown>("Dropdown");
            // dropdown.Q("Selection").style.flexDirection = reverse ? FlexDirection.RowReverse : FlexDirection.Row;
            _dropdownItemsSetter.SetItems(dropdown, dropdownProvider);

            //SetDropdownItems(dropdown, dropdownProvider, reverse, displayItemText);
            // _tooltipRegistrar.Register(dropdown, () => GetTooltipText(dropdownProvider));

            return visualElement;
        }

        public Button MakeMinimizerButton(VisualElement controlledContainer)
        {
            return MakeMinimizerButton(new List<VisualElement>() { controlledContainer });
        }

        public VisualElement MakeMinimizerButtonRow(VisualElement controlledContainer)
        {
            return MakeMinimizerButtonRow(new List<VisualElement>() { controlledContainer });
        }

        /// <summary>
        /// The first element's current display style is used as marker for correct DisplayStyle to apply
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public Button MakeMinimizerButton(IEnumerable<VisualElement> elements)
        {
            return MakeMinusButton(delegate
            {
                // Use the first element as holder of the state for all affected elements
                var current = elements.First().style.display;
                foreach (var e in elements)
                {
                    e.style.display = current == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
                }
            });
        }

        /// <summary>
        /// The first element's current display style is used as marker for correct DisplayStyle to apply
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public VisualElement MakeMinimizerButtonRow(IEnumerable<VisualElement> elements)
        {
            var root = MakeContainer(FlexDirection.Row, Align.Center, Justify.FlexEnd);
            var button = MakeMinimizerButton(elements);
            button.style.scale = new Scale(new Vector2(0.5f, 0.5f));
            button.style.flexGrow = 0;
            root.Add(button);
            return root;
        }
    }
}