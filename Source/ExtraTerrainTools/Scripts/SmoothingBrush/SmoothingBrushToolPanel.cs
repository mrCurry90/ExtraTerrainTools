using System;
using Timberborn.SingletonSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace TerrainTools.SmoothingBrush
{
    public class SmoothingBrushToolPanel : ITerrainToolFragment
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
        
        public SmoothingBrushToolPanel(TerrainToolPanelFactory toolPanelFactory, EventBus eventBus, SmoothingBrushTool tool)
        : base(eventBus, typeof(SmoothingBrushTool))
        {
            _elementFactory = toolPanelFactory;
            _tool = tool;
            ToolOrder = 10;
        }

        public override VisualElement BuildToolPanelContent()
        {
            _toolPanel = _elementFactory.MakeTemplatePanel( FlexDirection.Column, Align.Stretch );
            // var header  = _elementFactory.MakeContainer( FlexDirection.Column, Align.Stretch );
            var content = _elementFactory.MakeContainer( FlexDirection.Column, Align.Stretch );
            var footer = _elementFactory.MakeContainer( FlexDirection.Column, Align.Stretch );
            
            // _toolPanel.Add(header);
            _toolPanel.Add(content);
            _toolPanel.Add(footer);


            // content.Add(_elementFactory.MakeLabel("<b>Smoothing Tool</b>", TextAnchor.MiddleCenter));

            content.Add(
                MakeParameterSlider(
                    ref _sizeSlider, ref _sizeSliderValue, 
                    "Brush size:", 
                    delegate {
                        _sizeSlider.UpdateAsInt(_sizeSliderValue);
                        _tool.Size = (int)_sizeSlider.value;
                    }, 
                    1, 8, 4
                )
            );

            content.Add(
                MakeParameterSlider(
                    ref _sampleSizeSlider, ref _sampleSizeSliderValue, 
                    "Sample size:", 
                    delegate {
                        _sampleSizeSlider.UpdateAsInt(_sampleSizeSliderValue);
                        _tool.SampleSize = (int)_sampleSizeSlider.value;
                    }, 
                    1, 3, 3
                )
            );

            content.Add(
                MakeParameterSlider(
                    ref _forceSlider, ref _forceSliderValue, 
                    "Force:", 
                    delegate {
                        _forceSlider.UpdateAsPercent(_forceSliderValue);
                        _tool.Force = _forceSlider.value;
                    }, 
                    0.1f, 1f, 1f
                )
            );

            var checkboxRow = _elementFactory.MakeContainer( FlexDirection.Row, Align.Center, Justify.FlexStart );
            content.Add(checkboxRow);

            checkboxRow.Add(_elementFactory.MakeLabel("Brush shape:"));

            _squareShapeToggle = _elementFactory.MakeToggle( "Square", 
                delegate {
                    UpdateShapeToggles(!_squareShapeToggle.value);
                }
            );
            _circularShapeToggle = _elementFactory.MakeToggle( "Circle", 
                delegate {
                    UpdateShapeToggles(_circularShapeToggle.value);
                }
            );

            checkboxRow.Add(_squareShapeToggle);
            checkboxRow.Add(_circularShapeToggle);

            var footerRow = _elementFactory.MakeContainer( FlexDirection.Row, Align.Center, Justify.FlexEnd );
            footer.Add(footerRow);

            footerRow.Add(
                _elementFactory.MakeButton("Reset", delegate
                {
                    SetDefaultOptions();
                })
            );

            SetDefaultOptions();
            
            return _toolPanel;
        }

        private VisualElement MakeParameterSlider(ref Slider sliderRef, ref Label labelRef, string label, Action action, float min, float max, float initial)
        {
            VisualElement _sliderContainer = _elementFactory.MakeSlider( label, action, min, max, initial );
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
            _sizeSlider.value       = 4;
            _sampleSizeSlider.value = 3;
            _forceSlider.value      = 1f;
            
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