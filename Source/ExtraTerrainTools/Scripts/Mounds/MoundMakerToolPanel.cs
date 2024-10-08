using System;
using Timberborn.KeyBindingSystemUI;
using Timberborn.SingletonSystem;
using UnityEngine;
using UnityEngine.UIElements;

using TextFormat = TerrainTools.TerrainToolPanelFactory.TextFormat;

namespace TerrainTools.MoundMaker
{
    public class MoundMakerToolPanel : ITerrainToolFragment
    {
        private readonly TerrainToolPanelFactory _elementFactory;
        private readonly MoundMakerTool _tool;
        private VisualElement _toolPanel;

        private Slider _peakHeightSlider = null;
        private Label _peakHeightSliderValue = null;
        private Slider _maxAdjustSlider = null;
        private Label _maxAdjustSliderValue = null;       
        private Slider _radialNoiseSlider = null;
        private Label _radialNoiseSliderValue = null;
        private Slider _radialNoiseOctavesSlider = null;
        private Label _radialNoiseOctavesSliderValue = null;
        private Slider _radialNoiseFreqSlider = null;
        private Label _radialNoiseFreqSliderValue = null;
        private Slider _octavesSlider = null;
        private Label _octavesSliderValue = null;
        private Slider _ampSlider = null;
        private Label _ampSliderValue = null;
        private Slider _freqSlider = null;
        private Label _freqSliderValue = null;
        private Label _seedLabel = null;
        private Toggle _seedLockToggle = null;

        public MoundMakerToolPanel(TerrainToolPanelFactory toolPanelFactory, EventBus eventBus, MoundMakerTool tool)
        : base(eventBus, typeof(MoundMakerTool))
        {
            _elementFactory = toolPanelFactory;
            _tool = tool;
            _tool.SeedUpdated += (o,s) => {
                UpdateSeedLabel(s);
            };
            ToolOrder = 30;
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

            var shapeRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceBetween);
            content.Add(shapeRow);

            var cycleSeedTip = _elementFactory.MakeLabel("Tab = Cycle seed", TextAnchor.MiddleLeft, TextFormat.Italic);
            var digModeTip = _elementFactory.MakeLabel("Ctrl (Hold) = Dig Mode", TextAnchor.MiddleRight, TextFormat.Italic);
            shapeRow.Add(cycleSeedTip);            
            shapeRow.Add(digModeTip);
            

            content.Add(_elementFactory.MakeLabel("Shape", TextAnchor.MiddleCenter, TextFormat.Bold));            
            int maxHeight = MoundMakerTool.GetMaxHeight();
            content.Add(
                MakeParameterSlider(
                    ref _peakHeightSlider, ref _peakHeightSliderValue, 
                    "Peak height:", 
                    delegate { 
                        _peakHeightSlider.UpdateAsInt(_peakHeightSliderValue);
                        _tool.PeakHeight = (int)_peakHeightSlider.value;
                    }, 
                    1, maxHeight, 8
                )
            );

            content.Add(
                MakeParameterSlider(
                    ref _maxAdjustSlider, ref _maxAdjustSliderValue, 
                    "Max change:", 
                    delegate { 
                        _maxAdjustSlider.UpdateAsInt(_maxAdjustSliderValue);
                        _tool.MaxAdjust = (int)_maxAdjustSlider.value;
                    }, 
                    1, maxHeight, 16
                )
            );

            content.Add(_elementFactory.MakeLabel("Radial Noise", TextAnchor.MiddleCenter, TextFormat.Bold));
            // Radial Noise scale
            content.Add(
                MakeParameterSlider(
                    ref _radialNoiseSlider, ref _radialNoiseSliderValue, 
                    "Scale:", 
                    delegate {
                        _radialNoiseSlider.UpdateAsPercent(_radialNoiseSliderValue);
                        _tool.MinWidthScale = 1 - _radialNoiseSlider.value;

                        if( _radialNoiseSlider.value > 0) 
                        {
                            _radialNoiseFreqSlider.SetEnabled(true);
                            _radialNoiseOctavesSlider.SetEnabled(true);
                        }
                        else
                        {
                            _radialNoiseFreqSlider.SetEnabled(false);
                            _radialNoiseOctavesSlider.SetEnabled(false);
                        }
                    }, 
                    0, 1, 0.5f
                )
            );
            // Radial Noise frequency
            content.Add( 
                MakeParameterSlider(
                    ref _radialNoiseFreqSlider, ref _radialNoiseFreqSliderValue, 
                    "Frequency:", 
                    delegate { 
                        _radialNoiseFreqSlider.UpdateAsInt(_radialNoiseFreqSliderValue);
                        _tool.RadialNoiseFreqency = _radialNoiseFreqSlider.value;
                    }, 
                    1, 8, 4
                )             
            );
            // Radial Noise detail
            content.Add(
                MakeParameterSlider(
                    ref _radialNoiseOctavesSlider, ref _radialNoiseOctavesSliderValue, 
                    "Detail:", 
                    delegate {
                        _radialNoiseOctavesSlider.UpdateAsInt(_radialNoiseOctavesSliderValue);
                        _tool.RadialNoiseOctaves = (int)_radialNoiseOctavesSlider.value;
                    },
                    1, 8, 3
                )             
            );    
            

            content.Add(_elementFactory.MakeLabel("Vertical Noise", TextAnchor.MiddleCenter, TextFormat.Bold));
            // Noise amplitude
            content.Add( 
                MakeParameterSlider(
                    ref _ampSlider, ref _ampSliderValue, 
                    "Amplitude:", 
                    delegate { 
                        _ampSlider.Update(_ampSliderValue);
                        _tool.VertNoiseAmp = _ampSlider.value;

                        if( _ampSlider.value > 0) 
                        {
                            _octavesSlider.SetEnabled(true);
                            _freqSlider.SetEnabled(true);
                        }
                        else
                        {
                            _octavesSlider.SetEnabled(false);
                            _freqSlider.SetEnabled(false);
                        }
                    }, 
                    0, 16, 2
                )             
            );

            // Noise frequency
            content.Add( 
                MakeParameterSlider(
                    ref _freqSlider, ref _freqSliderValue, 
                    "Frequency:", 
                    delegate { 
                        _freqSlider.Update(_freqSliderValue);
                        _tool.VertNoiseFreq = _freqSlider.value;
                    }, 
                    1, 16, 3
                )             
            );

            content.Add(
                MakeParameterSlider(
                    ref _octavesSlider, ref _octavesSliderValue, 
                    "Detail:", 
                    delegate {
                        _octavesSlider.UpdateAsInt(_octavesSliderValue);
                        _tool.VertNoiseOctaves = (int)_octavesSlider.value;
                    },
                    1, 8, 3
                )             
            );            



            var footerRow = _elementFactory.MakeContainer( FlexDirection.Row, Align.Center, Justify.SpaceBetween );
            footer.Add(footerRow);

            var seedRow = _elementFactory.MakeContainer( FlexDirection.Row, Align.Center, Justify.FlexStart );
            _seedLockToggle = _elementFactory.MakeToggle("Randomize", delegate 
                {
                    _tool.AutoReseed = _seedLockToggle.value;
                },
                true
            );
            seedRow.Add(_seedLockToggle);

            _seedLabel = _elementFactory.MakeLabel("Error", TextAnchor.MiddleLeft);
            seedRow.Add(_seedLabel);

            footerRow.Add(seedRow);
            footerRow.Add(
                _elementFactory.MakeButton("Reset", delegate
                {
                    SetDefaultOptions();
                })
            );

            SetDefaultOptions();
            UpdateSeedLabel("None");

            return _toolPanel;
        }

        private VisualElement MakeParameterSlider(ref Slider sliderRef, ref Label labelRef, string label, Action action, float min, float max, float initial)
        {
            VisualElement _sliderContainer = _elementFactory.MakeSlider( label, action, min, max, initial );
            sliderRef = _sliderContainer.Q("Slider") as Slider;
            labelRef = _sliderContainer.Q("SliderValue") as Label;

            return _sliderContainer;
        }

        private void SetDefaultOptions()
        {
            _peakHeightSlider.value             = 8;
            _maxAdjustSlider.value              = 16;
            _radialNoiseSlider.value            = 0.5f;
            _radialNoiseFreqSlider.value        = 3;
            _radialNoiseOctavesSlider.value     = 4;
            _ampSlider.value                    = 2;
            _freqSlider.value                   = 5;
            _octavesSlider.value                = 3;
            _seedLockToggle.value               = true;

            _peakHeightSlider.UpdateAsInt(_peakHeightSliderValue);
            _maxAdjustSlider.UpdateAsInt(_maxAdjustSliderValue);
            _radialNoiseSlider.UpdateAsPercent(_radialNoiseSliderValue);
            _radialNoiseFreqSlider.UpdateAsInt(_radialNoiseFreqSliderValue);
            _radialNoiseOctavesSlider.UpdateAsInt(_radialNoiseOctavesSliderValue);
            _octavesSlider.UpdateAsInt(_octavesSliderValue);
            _ampSlider.Update(_ampSliderValue);
            _freqSlider.Update(_freqSliderValue);

            _tool.PeakHeight            = (int)_peakHeightSlider.value;
            _tool.MaxAdjust             = (int)_maxAdjustSlider.value;
            _tool.MinWidthScale         = 1 - _radialNoiseSlider.value;
            _tool.RadialNoiseFreqency   = _radialNoiseFreqSlider.value;
            _tool.RadialNoiseOctaves    = (int)_radialNoiseOctavesSlider.value;
            _tool.VertNoiseAmp          = _ampSlider.value;
            _tool.VertNoiseFreq         = _freqSlider.value;
            _tool.VertNoiseOctaves      = (int)_octavesSlider.value;
            _tool.AutoReseed            = _seedLockToggle.value;
        }
        
        private void UpdateSeedLabel(string text)
        {
            if (_seedLabel != null)
                _seedLabel.text = "Seed: " + text;
        }

    }
}