using System;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using UnityEngine.UIElements;

namespace TerrainTools.SmoothingBrush
{
    public class SmoothingBrushToolPanel : TerrainToolFragment
    {
        private readonly TerrainToolPanelFactory _elementFactory;
        private readonly SmoothingBrushTool _tool;
        private VisualElement _toolPanel;

        private Slider _sizeSlider = null;  // Size
        private Label _sizeSliderValue = null;
        private Slider _sampleSizeSlider = null; // SampleSize
        private Label _sampleSizeSliderValue = null;
        private Slider _forceSlider = null;  // Strength
        private Label _forceSliderValue = null;

        private Toggle _circularShapeToggle = null;   // Circular
        private Toggle _squareShapeToggle = null;   // Circular

        private readonly string _keyBrushSizeLabel = "TerrainTools.Smoothing.Slider.Brush.Size"; // Brush size
        private readonly string _keySampleSizeLabel = "TerrainTools.Smoothing.Slider.Sample.Size"; // Sample size
        private readonly string _keyForceLabel = "TerrainTools.Smoothing.Slider.Force"; // Force
        private readonly string _keyBrushShapeLabel = "TerrainTools.Smoothing.ToggleGroup.Shape"; // Brush shape
        private readonly string _keyCbSquareLabel = "TerrainTools.Smoothing.Shape.Square"; // Square
        private readonly string _keyCbCircleLabel = "TerrainTools.Smoothing.Shape.Circle"; // Circle
        private readonly string _keyResetButtonLabel = "TerrainTools.Common.Button.Reset"; // Reset

        public SmoothingBrushToolPanel(TerrainToolPanelFactory toolPanelFactory, EventBus eventBus, SmoothingBrushTool tool, ILoc loc)
        : base(eventBus, typeof(SmoothingBrushTool), loc)
        {
            _elementFactory = toolPanelFactory;
            _tool = tool;
            ToolOrder = 10;
        }

        public override VisualElement BuildToolPanelContent()
        {
            _toolPanel = _elementFactory.MakeToolPanel(FlexDirection.Column, Align.Stretch);
            // var header  = _elementFactory.MakeContainer( FlexDirection.Column, Align.Stretch );
            var content = _elementFactory.MakeContainer(FlexDirection.Column, Align.Stretch);
            var footer = _elementFactory.MakeContainer(FlexDirection.Column, Align.Stretch);

            // _toolPanel.Add(header);
            _toolPanel.Add(content);
            _toolPanel.Add(footer);


            // content.Add(_elementFactory.MakeLabel("<b>Smoothing Tool</b>", TextAnchor.MiddleCenter));

            content.Add(
                MakeParameterSlider(
                    ref _sizeSlider, ref _sizeSliderValue,
                    _loc.T(_keyBrushSizeLabel) + ":",
                    delegate
                    {
                        _sizeSlider.UpdateAsInt(_sizeSliderValue);
                        _tool.Size = (int)_sizeSlider.value;
                    },
                    1, 8, 4
                )
            );

            content.Add(
                MakeParameterSlider(
                    ref _sampleSizeSlider, ref _sampleSizeSliderValue,
                    _loc.T(_keySampleSizeLabel) + ":",
                    delegate
                    {
                        _sampleSizeSlider.UpdateAsInt(_sampleSizeSliderValue);
                        _tool.SampleSize = (int)_sampleSizeSlider.value;
                    },
                    1, 3, 3
                )
            );

            content.Add(
                MakeParameterSlider(
                    ref _forceSlider, ref _forceSliderValue,
                    _loc.T(_keyForceLabel) + ":",
                    delegate
                    {
                        _forceSlider.UpdateAsPercent(_forceSliderValue);
                        _tool.Force = _forceSlider.value;
                    },
                    0.5f, 0.75f, 1f
                )
            );

            var checkboxRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.FlexStart);
            content.Add(checkboxRow);

            checkboxRow.Add(_elementFactory.MakeLabel(_loc.T(_keyBrushShapeLabel) + ":"));

            _squareShapeToggle = _elementFactory.MakeToggle(_loc.T(_keyCbSquareLabel),
                delegate ()
                {
                    UpdateShapeToggles(!_squareShapeToggle.value);
                }
            );
            _circularShapeToggle = _elementFactory.MakeToggle(_loc.T(_keyCbCircleLabel),
                delegate ()
                {
                    UpdateShapeToggles(_circularShapeToggle.value);
                }
            );

            checkboxRow.Add(_squareShapeToggle);
            checkboxRow.Add(_circularShapeToggle);

            var footerRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.FlexEnd);
            footer.Add(footerRow);

            footerRow.Add(
                _elementFactory.MakeTextButton(_loc.T(_keyResetButtonLabel), delegate
                {
                    SetDefaultOptions();
                })
            );

            SetDefaultOptions();

            return _toolPanel;
        }

        private VisualElement MakeParameterSlider(ref Slider sliderRef, ref Label labelRef, string label, Action action, float min, float max, float initial)
        {
            VisualElement _sliderContainer = _elementFactory.MakeSlider(label, action, min, max, initial);
            sliderRef = _sliderContainer.Q("Slider") as Slider;
            labelRef = _sliderContainer.Q("SliderValue") as Label;

            return _sliderContainer;
        }

        private void UpdateShapeToggles(bool circular)
        {
            _tool.Circular = circular;
            _squareShapeToggle.value = !circular;
            _circularShapeToggle.value = circular;
        }

        private void SetDefaultOptions()
        {
            _sizeSlider.value = 4;
            _sampleSizeSlider.value = 3;
            _forceSlider.value = 1f;

            UpdateShapeToggles(true);

            _sizeSlider.UpdateAsInt(_sizeSliderValue);
            _sampleSizeSlider.UpdateAsInt(_sampleSizeSliderValue);
            _forceSlider.UpdateAsPercent(_forceSliderValue);

            _tool.Size = (int)_sizeSlider.value;
            _tool.SampleSize = (int)_sampleSizeSlider.value;
            _tool.Force = _forceSlider.value;
        }
    }
}