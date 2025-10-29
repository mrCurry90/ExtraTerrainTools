using System;
using System.Collections.Generic;
using System.Linq;
using Timberborn.CoreUI;
using Timberborn.DropdownSystem;
using Timberborn.SingletonSystem;
using UnityEngine;
using UnityEngine.UIElements;
namespace TerrainTools
{
    public class TerrainToolPanelFactory : ILoadableSingleton
    {
        #region TextFormat
        [Flags]
        public enum TextFormat
        {
            None = 0,
            Bold = 1,
            Italic = 2
        }
        #endregion

        #region Boilerplate
        // private readonly VisualElementLoader _loader;
        private readonly DropdownItemsSetter _dropdownItemsSetter;
        private readonly DropdownListDrawer _dropdownListDrawer;

        // private static readonly string brushShapePath = "MapEditor/ToolPanel/BrushShapePanel";
        // private static readonly string brushSizePath = "MapEditor/ToolPanel/BrushSizePanel";
        // private static readonly string thumbnailPath = "MapEditor/ToolPanel/ThumbnailCapturingPanel";
        // private static readonly string spawningPath = "MapEditor/ToolPanel/NaturalResourceSpawningBrushPanel";
        // private static readonly string togglePath = "MapEditor/ToolPanel/ToolPanelToggle";
        // private static readonly string newMapBoxPath = "Options/NewMapBox";
        // private static readonly string dropdownPath = "Game/BatchControl/DropdownBatchControlRowItem";
        // private static readonly string mapSelection = "Common/MapSelection";


        public TerrainToolPanelFactory(
            // VisualElementLoader visualElementLoader,
            DropdownItemsSetter dropdownItemsSetter,
            DropdownListDrawer dropdownListDrawer
        )
        {
            // _loader = visualElementLoader;
            _dropdownItemsSetter = dropdownItemsSetter;
            _dropdownListDrawer = dropdownListDrawer;
        }

        public void Load()
        {
        }
        #endregion

        #region Containers
        public NineSliceVisualElement MakeToolPanel(FlexDirection flexDirection = FlexDirection.Column, Align alignItems = Align.Center, Justify justifyContent = Justify.FlexStart)
        {
            var container = new NineSliceVisualElement();
            container.AddToClassList("tool-panel-item--map-editor");
            container.AddToClassList("bg-box--green");

            SetElementLayout(container, flexDirection, alignItems, justifyContent);
            return container;
        }

        public VisualElement MakeContainer(FlexDirection flexDirection = FlexDirection.Column, Align alignItems = Align.Center, Justify justifyContent = Justify.FlexStart)
        {
            VisualElement container = new();
            SetElementLayout(container, flexDirection, alignItems, justifyContent);
            return container;
        }
        public NineSliceVisualElement MakeDescriptionContainer(FlexDirection flexDirection = FlexDirection.Column, Align alignItems = Align.Center, Justify justifyContent = Justify.FlexStart)
        {
            var container = new NineSliceVisualElement();
            container.AddToClassList("map-selection__description-background");
            SetElementLayout(container, flexDirection, alignItems, justifyContent);
            return container;
        }

        #endregion

        #region Labels and Textfields
        public Label MakeLabel(string text)
        {
            Label label = new();
            label.AddToClassList("game-text-normal");
            label.AddToClassList("tool-panel-item__label");
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
        public TextField MakeTextField(Action changeAction = null)
        {
            NineSliceTextField textField = new();
            textField.AddToClassList("text-field");

            if (changeAction != null)
            {
                textField.RegisterValueChangedCallback(delegate
                {
                    changeAction();
                });
            }

            return textField;
        }
        #endregion

        #region Buttons
        private Button CreateTextButton(string text)
        {
            LocalizableButton button = new();
            button.AddToClassList("button-game");
            button.AddToClassList("game-text-normal");
            button.SetMargin(2);
            button.SetPadding(4, 8);
            button.text = text;
            return button;
        }

        public Button MakeTextButton<T>(string text, Action<Button, T> action, T arg)
        {
            var button = CreateTextButton(text);
            button.RegisterCallback<ClickEvent>(delegate
            {
                action(button, arg);
            });
            return button;
        }

        public Button MakeTextButton<T>(string text, Action<Button> action)
        {
            var button = CreateTextButton(text);
            button.RegisterCallback<ClickEvent>(delegate
            {
                action(button);
            });
            return button;
        }

        public Button MakeTextButton(string text, Action action)
        {
            var button = CreateTextButton(text);
            button.RegisterCallback<ClickEvent>(delegate
            {
                action();
            });
            return button;
        }

        private Button CreateSquareButton()
        {
            Button button = new();
            button.AddToClassList("button-square");
            return button;
        }

        public Button MakeMinusButton(Action action)
        {
            var button = CreateSquareButton();
            button.AddToClassList("button-minus");
            button.RegisterCallback<ClickEvent>(delegate
            {
                action();
            });
            return button;
        }
        public Button MakePlusButton(Action action)
        {
            var button = CreateSquareButton();
            button.AddToClassList("button-plus");
            button.RegisterCallback<ClickEvent>(delegate
            {
                action();
            });
            return button;
        }

        public VisualElement MakeMinimizerButton(VisualElement controlledContainer)
        {
            return MakeMinimizerButton(new List<VisualElement>() { controlledContainer });
        }

        /// <summary>
        /// The first element's current display style is used as marker for correct DisplayStyle to apply
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public VisualElement MakeMinimizerButton(IEnumerable<VisualElement> elements)
        {
            var root = MakeContainer(FlexDirection.Row, Align.Center, Justify.FlexEnd);
            var scale = new Scale(new Vector2(0.75f, 0.75f));
            var button = CreateSquareButton();
            button.AddToClassList("button-arrow-down");

            button.style.scale = scale;
            button.style.flexGrow = 0;
            button.style.flexShrink = 0;

            button.RegisterCallback<ClickEvent>(delegate
            {
                // Use the first element as holder of the state for all affected elements
                var current = elements.First().style.display;
                if (current == DisplayStyle.None)
                {
                    button.RemoveFromClassList("button-arrow-up");
                    button.AddToClassList("button-arrow-down");
                }
                else
                {
                    button.RemoveFromClassList("button-arrow-down");
                    button.AddToClassList("button-arrow-up");
                }

                // Update all managed elements
                foreach (var e in elements)
                {
                    e.style.display = current == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
                }
            });

            root.Add(button);
            return root;
        }
        #endregion

        #region Toggles
        private Toggle CreateToggle(string text, bool value)
        {
            Toggle toggle = new();
            toggle.AddToClassList("game-toggle");
            toggle.AddToClassList("tool-panel-toggle");
            toggle.text = text;
            toggle.value = value;

            return toggle;
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
            var toggle = CreateToggle(text, defaultState);
            toggle.RegisterValueChangedCallback(delegate
            {
                toggleAction();
            });

            return toggle;
        }

        public Toggle MakeToggle(string text, Action<Toggle> toggleAction, bool defaultState = false)
        {
            var toggle = CreateToggle(text, defaultState);
            toggle.RegisterValueChangedCallback(delegate
            {
                toggleAction(toggle);
            });

            return toggle;
        }
        #endregion

        #region Sliders
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
            // int[] path = { 1 };
            // VisualElement container = LoadChild(spawningPath, path);
            // Slider slider = container.Q("Slider") as Slider;
            VisualElement container = new();
            container.AddToClassList("resource-spawning-brush-panel__slider-wrapper");

            Slider slider = new() { name = "Slider" };
            slider.AddToClassList("tool-panel-slider");

            Label sliderValue = new() { name = "SliderValue" };
            sliderValue.AddToClassList("game-text-normal");
            sliderValue.AddToClassList("resource-spawning-brush-panel__slider-value");

            container.Add(slider);
            container.Add(sliderValue);

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
        #endregion


        #region Dropdowns
        private (VisualElement, Dropdown) CreateDropdown()
        {
            NineSliceVisualElement row = new();
            row.AddToClassList("batch-control-box__row-item-group");
            row.AddToClassList("dropdown-batch-control-row-item");

            Dropdown dropdown = new()
            {
                name = "Dropdown"
            };
            dropdown.Initialize(_dropdownListDrawer);

            row.Add(dropdown);

            return (row, dropdown);
        }
        public VisualElement MakeDropdown(IDropdownProvider dropdownProvider)
        {
            var (row, dropdown) = CreateDropdown();
            _dropdownItemsSetter.SetItems(dropdown, dropdownProvider);
            return row;
        }

        public VisualElement MakeDropdown(IExtendedDropdownProvider dropdownProvider)
        {
            var (row, dropdown) = CreateDropdown();
            _dropdownItemsSetter.SetItems(dropdown, dropdownProvider);

            return row;
        }
        #endregion

        #region Private members
        private void SetElementLayout(VisualElement container, FlexDirection flexDirection, Align alignItems, Justify justifyContent)
        {
            container.style.flexDirection = flexDirection;
            container.style.alignItems = alignItems;
            container.style.justifyContent = justifyContent;
        }
        #endregion
    }
}