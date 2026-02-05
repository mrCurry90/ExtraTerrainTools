using System;
using Timberborn.KeyBindingSystemUI;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using UnityEngine;
using UnityEngine.UIElements;

using TextFormat = TerrainTools.TerrainToolPanelFactory.TextFormat;

namespace TerrainTools.MoundMaker
{
    public class MoundMakerToolPanel : TerrainToolFragment
    {
        private readonly TerrainToolPanelFactory _elementFactory;
        private readonly MoundMakerTool _tool;
        private readonly InputBindingDescriber _inputDescriber;
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

        private readonly string _keyCycleSeedTip = "TerrainTools.MoundMaker.Hint.CycleSeed"; // "{0} = Cycle seed"
        private readonly string _keyDigModeTip = "TerrainTools.MoundMaker.Hint.DigMode"; // "{0} (Hold) = Dig Mode"

        private readonly string _keyShapeHeader = "TerrainTools.MoundMaker.Header.Shape"; // "Shape" 
        private readonly string _keyPeakHeightSliderLabel = "TerrainTools.MoundMaker.Slider.PeakHeight"; // "Peak height"
        private readonly string _keyMaxChangeSliderLabel = "TerrainTools.MoundMaker.Slider.MaxChange"; // "Max change"

        private readonly string _keyRadialNoiseHeader = "TerrainTools.MoundMaker.Header.RadialNoise"; // "Radial Noise"
        private readonly string _keyRadialScaleSliderLabel = "TerrainTools.MoundMaker.Slider.Radial.Scale"; // "Scale"
        private readonly string _keyRadialFreqSliderLabel = "TerrainTools.MoundMaker.Slider.Radial.Frequency"; // "Frequency"
        private readonly string _keyRadialDetailSliderLabel = "TerrainTools.MoundMaker.Slider.Radial.Detail"; // "Detail"

        private readonly string _keyVerticalNoiseHeader = "TerrainTools.MoundMaker.Header.VerticalNoise"; // "Vertical Noise"
        private readonly string _keyVerticalAmpSliderLabel = "TerrainTools.MoundMaker.Slider.Vertical.Amplitude"; // Amplitude
        private readonly string _keyVerticalFreqSliderLabel = "TerrainTools.MoundMaker.Slider.Vertical.Frequency"; // Frequency
        private readonly string _keyVerticalDetailSliderLabel = "TerrainTools.MoundMaker.Slider.Vertical.Detail"; // Detail

        private readonly string _keyRandomizeCheckboxLabel = "TerrainTools.MoundMaker.Checkbox.Randomize"; // Randomize
        private readonly string _keySeedDisplayLabel = "TerrainTools.MoundMaker.Seed.Label"; // Seed
        private readonly string _keyResetButtonLabel = "TerrainTools.Common.Button.Reset"; // Reset

        public MoundMakerToolPanel(TerrainToolPanelFactory toolPanelFactory, EventBus eventBus, MoundMakerTool tool, InputBindingDescriber inputBindingDescriber, ILoc loc)
        : base(eventBus, typeof(MoundMakerTool), loc)
        {
            _inputDescriber = inputBindingDescriber;
            _elementFactory = toolPanelFactory;
            _tool = tool;
            _tool.SeedUpdated += (o, s) =>
            {
                UpdateSeedLabel(s);
            };

            ToolOrder = 30;
        }

        #region Build Tool Panel
        public override VisualElement BuildToolPanelContent()
        {
            _toolPanel = _elementFactory.MakeToolPanel(FlexDirection.Column, Align.Stretch);
            var header = _elementFactory.MakeContainer(FlexDirection.Column, Align.Stretch);
            var content = _elementFactory.MakeContainer(FlexDirection.Column, Align.Stretch);
            var footer = _elementFactory.MakeContainer(FlexDirection.Column, Align.Stretch);

            _toolPanel.Add(header);
            _toolPanel.Add(content);
            _toolPanel.Add(footer);
            #region header
            header.Add(_elementFactory.MakeMinimizerButton(content));

            var tipsRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceBetween);
            header.Add(tipsRow);

            var reseedKey = _inputDescriber.GetInputBindingText(MoundMakerTool.ReseedKeybind);
            var digModeKey = _inputDescriber.GetInputBindingText(MoundMakerTool.FlipModeKeybind);

            var cycleSeedTip = _elementFactory.MakeLabel(
                string.Format(_loc.T(_keyCycleSeedTip), reseedKey),
                TextAnchor.MiddleLeft, TextFormat.Italic);
            var digModeTip = _elementFactory.MakeLabel(
                string.Format(_loc.T(_keyDigModeTip), digModeKey),
                TextAnchor.MiddleRight, TextFormat.Italic);

            tipsRow.Add(cycleSeedTip);
            tipsRow.Add(digModeTip);
            #endregion header
            #region Shape
            content.Add(_elementFactory.MakeLabel(_loc.T(_keyShapeHeader), TextAnchor.MiddleCenter, TextFormat.Bold));
            int maxHeight = _tool.MaxTerrainHeight;
            content.Add(
                MakeParameterSlider(
                    ref _peakHeightSlider, ref _peakHeightSliderValue,
                    _loc.T(_keyPeakHeightSliderLabel) + ":",
                    delegate
                    {
                        _peakHeightSlider.UpdateAsInt(_peakHeightSliderValue);
                        _tool.PeakHeight = (int)_peakHeightSlider.value;
                    },
                    1, maxHeight, 8
                )
            );

            content.Add(
                MakeParameterSlider(
                    ref _maxAdjustSlider, ref _maxAdjustSliderValue,
                    _loc.T(_keyMaxChangeSliderLabel) + ":",
                    delegate
                    {
                        _maxAdjustSlider.UpdateAsInt(_maxAdjustSliderValue);
                        _tool.MaxAdjust = (int)_maxAdjustSlider.value;
                    },
                    1, maxHeight, 16
                )
            );
            #endregion Shape

            #region Radial noise
            content.Add(_elementFactory.MakeLabel(_loc.T(_keyRadialNoiseHeader), TextAnchor.MiddleCenter, TextFormat.Bold));
            // Radial Noise scale
            content.Add(
                MakeParameterSlider(
                    ref _radialNoiseSlider, ref _radialNoiseSliderValue,
                    _loc.T(_keyRadialScaleSliderLabel) + ":",
                    delegate
                    {
                        _radialNoiseSlider.UpdateAsPercent(_radialNoiseSliderValue);
                        _tool.MinWidthScale = 1 - _radialNoiseSlider.value;

                        if (_radialNoiseSlider.value > 0)
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
                    _loc.T(_keyRadialFreqSliderLabel) + ":",
                    delegate
                    {
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
                    _loc.T(_keyRadialDetailSliderLabel) + ":",
                    delegate
                    {
                        _radialNoiseOctavesSlider.UpdateAsInt(_radialNoiseOctavesSliderValue);
                        _tool.RadialNoiseOctaves = (int)_radialNoiseOctavesSlider.value;
                    },
                    1, 8, 3
                )
            );
            #endregion Radial Noise
            #region Vertical Noise
            content.Add(_elementFactory.MakeLabel(_loc.T(_keyVerticalNoiseHeader), TextAnchor.MiddleCenter, TextFormat.Bold));
            // Noise amplitude
            content.Add(
                MakeParameterSlider(
                    ref _ampSlider, ref _ampSliderValue,
                    _loc.T(_keyVerticalAmpSliderLabel) + ":",
                    delegate
                    {
                        _ampSlider.Update(_ampSliderValue);
                        _tool.VertNoiseAmp = _ampSlider.value;

                        if (_ampSlider.value > 0)
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
                    _loc.T(_keyVerticalFreqSliderLabel) + ":",
                    delegate
                    {
                        _freqSlider.Update(_freqSliderValue);
                        _tool.VertNoiseFreq = _freqSlider.value;
                    },
                    1, 16, 3
                )
            );

            content.Add(
                MakeParameterSlider(
                    ref _octavesSlider, ref _octavesSliderValue,
                    _loc.T(_keyVerticalDetailSliderLabel) + ":",
                    delegate
                    {
                        _octavesSlider.UpdateAsInt(_octavesSliderValue);
                        _tool.VertNoiseOctaves = (int)_octavesSlider.value;
                    },
                    1, 8, 3
                )
            );
            #endregion Vertical Noise

            #region footer
            var footerRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceBetween);
            footer.Add(footerRow);

            var seedRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.FlexStart);
            _seedLockToggle = _elementFactory.MakeToggle(_loc.T(_keyRandomizeCheckboxLabel), delegate ()
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
                _elementFactory.MakeTextButton(_loc.T(_keyResetButtonLabel), delegate
                {
                    SetDefaultOptions();
                })
            );
            #endregion footer
            SetDefaultOptions();
            UpdateSeedLabel("None");

            return _toolPanel;
        }
        #endregion Build Tool Panel

        #region Support methods
        private VisualElement MakeParameterSlider(ref Slider sliderRef, ref Label labelRef, string label, Action action, float min, float max, float initial)
        {
            VisualElement _sliderContainer = _elementFactory.MakeSlider(label, action, min, max, initial);
            sliderRef = _sliderContainer.Q("Slider") as Slider;
            labelRef = _sliderContainer.Q("SliderValue") as Label;

            return _sliderContainer;
        }

        private void SetDefaultOptions()
        {
            _peakHeightSlider.value = 8;
            _maxAdjustSlider.value = 16;
            _radialNoiseSlider.value = 0.5f;
            _radialNoiseFreqSlider.value = 3;
            _radialNoiseOctavesSlider.value = 4;
            _ampSlider.value = 2;
            _freqSlider.value = 5;
            _octavesSlider.value = 3;
            _seedLockToggle.value = true;

            _peakHeightSlider.UpdateAsInt(_peakHeightSliderValue);
            _maxAdjustSlider.UpdateAsInt(_maxAdjustSliderValue);
            _radialNoiseSlider.UpdateAsPercent(_radialNoiseSliderValue);
            _radialNoiseFreqSlider.UpdateAsInt(_radialNoiseFreqSliderValue);
            _radialNoiseOctavesSlider.UpdateAsInt(_radialNoiseOctavesSliderValue);
            _octavesSlider.UpdateAsInt(_octavesSliderValue);
            _ampSlider.Update(_ampSliderValue);
            _freqSlider.Update(_freqSliderValue);

            _tool.PeakHeight = (int)_peakHeightSlider.value;
            _tool.MaxAdjust = (int)_maxAdjustSlider.value;
            _tool.MinWidthScale = 1 - _radialNoiseSlider.value;
            _tool.RadialNoiseFreqency = _radialNoiseFreqSlider.value;
            _tool.RadialNoiseOctaves = (int)_radialNoiseOctavesSlider.value;
            _tool.VertNoiseAmp = _ampSlider.value;
            _tool.VertNoiseFreq = _freqSlider.value;
            _tool.VertNoiseOctaves = (int)_octavesSlider.value;
            _tool.AutoReseed = _seedLockToggle.value;
        }

        private void UpdateSeedLabel(string text)
        {
            if (_seedLabel != null)
                _seedLabel.text = _loc.T(_keySeedDisplayLabel) + ": " + text;
        }
        #endregion Support methods
    }
}