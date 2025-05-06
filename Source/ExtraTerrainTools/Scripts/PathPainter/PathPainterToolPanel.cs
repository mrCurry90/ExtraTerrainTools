using System;
using Timberborn.SingletonSystem;
using Timberborn.DropdownSystem;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Timberborn.Localization;
using System.Linq;
using Timberborn.KeyBindingSystemUI;

namespace TerrainTools.PathPainter
{
    public class PathPainterToolPanel : ITerrainToolFragment
    {
        private readonly TerrainToolPanelFactory _elementFactory;
        private readonly PathPainterTool _tool;
        private readonly InputBindingDescriber _inputBindingDescriber;
        private VisualElement _toolPanel;

        private Slider _radiusSlider = null;
        private Label _radiusSliderValue = null;

        private Dropdown _leftSlopeDropdown = null;
        private Dropdown _rightSlopeDropdown = null;


        private Slider _leftNearHeightSlider = null;
        private Label _leftNearHeightSliderValue = null;
        private Slider _leftFarHeightSlider = null;
        private Label _leftFarHeightSliderValue = null;

        private Slider _rightNearHeightSlider = null;
        private Label _rightNearHeightSliderValue = null;
        private Slider _rightFarHeightSlider = null;
        private Label _rightFarHeightSliderValue = null;

        private LabelToggle _linkAllToggle = null;
        private List<ImageToggle> _linkToggles = new();

        private Texture2D _textureLinked;
        private Texture2D _textureUnlinked;
        private Texture2D _textureMarker;

        private readonly SlopeDropedownProvider _leftSlopeDropdownProvider;
        private readonly SlopeDropedownProvider _rightSlopeDropdownProvider;
        private readonly DropdownItemsSetter _dropdownItemsSetter;

        private Button[] _modeButtons;

        private readonly string _headerTipsTextFormat = "<i>{0}</i>";
        private readonly string _symmetryToggleTextFormat = "{0}: <b>{1}</b>";
        private readonly string _symmetryToggleHoveredFormat = "<u>{0}: <b>{1}</b></u>";

        private readonly string _keyTipAddMove = "TerrainTools.PathPainter.Tip.AddMove";
        private readonly string _keyTipDeleteHovered = "TerrainTools.PathPainter.Tip.DeleteHovered";
        private readonly string _keyTipDeleteMultiple = "TerrainTools.PathPainter.Tip.DeleteMultiple";
        private readonly string _keyButtonLine = "TerrainTools.PathPainter.Curve.Button.Line";
        private readonly string _keyButton3Points = "TerrainTools.PathPainter.Curve.Button.3Points";
        private readonly string _keyButton4Points = "TerrainTools.PathPainter.Curve.Button.4Points";
        private readonly string _keyButtonContinuous = "TerrainTools.PathPainter.Curve.Button.Continuous";
        private readonly string _keyToggleSingleSegment = "TerrainTools.PathPainter.Curve.Toggle.SingleSegment";
        private readonly string _keyToggleEndCaps = "TerrainTools.PathPainter.Curve.Toggle.DrawEndCaps";
        private readonly string _keySliderDistance = "TerrainTools.PathPainter.Curve.Slider.Distance";
        private readonly string _keySymmetryDescription = "TerrainTools.PathPainter.ToggleText.Symmetry";
        private readonly string _keySymmetryOn = "TerrainTools.PathPainter.ToggleText.Symmetry.On";
        private readonly string _keySymmetryOff = "TerrainTools.PathPainter.ToggleText.Symmetry.Off";
        private readonly string _keyButtonApply = "TerrainTools.Common.Button.Apply";
        private readonly string _keyButtonClear = "TerrainTools.PathPainter.Button.ClearAll";
        private readonly string _keyButtonReset = "TerrainTools.Common.Button.Reset";

        public PathPainterToolPanel(
            TerrainToolPanelFactory toolPanelFactory, EventBus eventBus, PathPainterTool tool, SlopeImageService imageService,
            TerrainToolsAssetService assetService, ILoc iloc, DropdownItemsSetter dropdownItemsSetter, InputBindingDescriber inputBindingDescriber
        ) : base(eventBus, typeof(PathPainterTool), iloc)
        {
            _inputBindingDescriber = inputBindingDescriber;
            _dropdownItemsSetter = dropdownItemsSetter;
            _elementFactory = toolPanelFactory;
            _tool = tool;
            ToolOrder = 20;

            _textureLinked = assetService.Fetch<Texture2D>("UI/linked", TerrainToolsAssetService.Folder.Textures);
            _textureUnlinked = assetService.Fetch<Texture2D>("UI/unlinked", TerrainToolsAssetService.Folder.Textures);
            _textureMarker = assetService.Fetch<Texture2D>("UI/tile_marker", TerrainToolsAssetService.Folder.Textures);

            _leftSlopeDropdownProvider = new(imageService, flipImages: false,
                () => imageService.BuildKey(_tool.LeftSlope.F1, _tool.LeftSlope.Direction),
                (s) =>
                {
                    if (Slope.TryBuildFromString(s, out var slope))
                    {
                        _tool.LeftSlope = slope;
                    }
                    else
                    {
                        Utils.Log("Right slope: Invalid string provided, cannot build Slope");
                    }
                }
            );

            _rightSlopeDropdownProvider = new(imageService, flipImages: true,
                () => imageService.BuildKey(_tool.RightSlope.F1, _tool.RightSlope.Direction),
                (s) =>
                {
                    if (Slope.TryBuildFromString(s, out var slope))
                    {
                        _tool.RightSlope = slope;
                    }
                    else
                    {
                        Utils.Log("Right slope: Invalid string provided, cannot build Slope");
                    }
                }
            );
        }

        public override VisualElement BuildToolPanelContent()
        {
            _linkToggles.Clear();

            _toolPanel = _elementFactory.MakeTemplatePanel(FlexDirection.Column, Align.Stretch);
            var header = _elementFactory.MakeContainer(FlexDirection.Column, Align.Stretch, Justify.FlexStart);
            var content = _elementFactory.MakeContainer(FlexDirection.Column, Align.Stretch);
            var footer = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceBetween);

            _toolPanel.Add(header);
            _toolPanel.Add(content);
            _toolPanel.Add(footer);

            header.Add(_elementFactory.MakeMinimizerButtonRow(content));

            var tipRow1 = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceEvenly);
            var tipRow2 = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceEvenly);
            tipRow1.Add(
                _elementFactory.MakeLabel(
                    string.Format(
                        _headerTipsTextFormat,
                        string.Format(_loc.T(_keyTipAddMove), _inputBindingDescriber.GetInputBindingText(PathPainterTool.AddMoveKeybind))
                    )
                )
            );

            tipRow2.Add(
                _elementFactory.MakeLabel(
                    string.Format(
                        _headerTipsTextFormat,
                        string.Format(_loc.T(_keyTipDeleteHovered), _inputBindingDescriber.GetInputBindingText(PathPainterTool.DeleteKeybind))
                    )
                )
            );

            tipRow2.Add(
                _elementFactory.MakeLabel(
                    string.Format(
                        _headerTipsTextFormat,
                        string.Format(_loc.T(_keyTipDeleteMultiple), _inputBindingDescriber.GetInputBindingText(PathPainterTool.DeleteKeybind))
                    )
                )
            );

            header.Add(tipRow1);
            header.Add(tipRow2);

            _modeButtons = new[]{
                _elementFactory.MakeButton(_loc.T(_keyButtonLine), OnModeSelect, SplineDrawerMode.Linear),
                _elementFactory.MakeButton(_loc.T(_keyButton3Points), OnModeSelect, SplineDrawerMode.Quadratic),
                _elementFactory.MakeButton(_loc.T(_keyButton4Points), OnModeSelect, SplineDrawerMode.Cubic),
                _elementFactory.MakeButton(_loc.T(_keyButtonContinuous), OnModeSelect, SplineDrawerMode.Continuous)
            };

            var modeButtonsRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceEvenly);
            modeButtonsRow.style.paddingTop = 10;
            modeButtonsRow.style.paddingBottom = 10;
            for (int i = 0; i < _modeButtons.Length; i++)
            {
                modeButtonsRow.Add(_modeButtons[i]);
            }

            var modeToggles = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceAround);
            modeToggles.Add(
                _elementFactory.MakeToggle(
                    _loc.T(_keyToggleSingleSegment),
                    delegate (Toggle toggle)
                    {
                        _tool.SimpleCurves = toggle.value;
                    },
                    _tool.SimpleCurves
                )
            );
            modeToggles.Add(
                _elementFactory.MakeToggle(
                    _loc.T(_keyToggleEndCaps),
                    delegate (Toggle toggle)
                    {
                        _tool.DrawEndCaps = toggle.value;
                    },
                    _tool.DrawEndCaps
                )
            );

            content.Add(modeButtonsRow);
            content.Add(modeToggles);

            content.Add(
                MakeParameterSlider(
                    ref _radiusSlider, ref _radiusSliderValue,
                    _loc.T(_keySliderDistance) + ":",
                    delegate
                    {
                        _radiusSlider.UpdateAsInt(_radiusSliderValue);
                        _tool.Radius = (int)_radiusSlider.value;
                    },
                    0, 16, 2
                )
            );
            var sideHeaderRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.Center);
            var slopeRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceEvenly);
            var nearHeightRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceAround);
            var farHeightRow = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceAround);

            var symmetryDesc = _loc.T(_keySymmetryDescription);
            var symmetryOn = _loc.T(_keySymmetryOn);
            var symmetryOff = _loc.T(_keySymmetryOff);

            sideHeaderRow.Add(_linkAllToggle = MakeTextToggle(
                textOn: string.Format(_symmetryToggleTextFormat, symmetryDesc, symmetryOn),
                textOff: string.Format(_symmetryToggleTextFormat, symmetryDesc, symmetryOff),
                textOnHoverWhileOn: string.Format(_symmetryToggleHoveredFormat, symmetryDesc, symmetryOn),
                textOnHoverWhileOff: string.Format(_symmetryToggleHoveredFormat, symmetryDesc, symmetryOff),
                (link) =>
                {
                    foreach (var l in _linkToggles)
                    {
                        l.value = link.value;
                    }
                },
                defaultState: true
            ));

            slopeRow.Add(
                MakeSlopeDropdown(ref _leftSlopeDropdown, _leftSlopeDropdownProvider, reverse: true)
            );

            var paddedLinkToggle = MakeLinkToggle(canBeToggledByOther: true, (link) =>
            {
                _rightSlopeDropdown.SetEnabled(!link.value);
                if (link.value)
                {
                    _rightSlopeDropdownProvider.SetValue(_leftSlopeDropdownProvider.GetValue());
                    _rightSlopeDropdown.RefreshContent();
                    _leftSlopeDropdown.ValueChanged += UpdateLinkedSlopeDropdown;
                }
                else
                {
                    _leftSlopeDropdown.ValueChanged -= UpdateLinkedSlopeDropdown;
                }
            });
            // paddedLinkToggle.style.marginLeft = 10;
            // paddedLinkToggle.style.marginRight = 10;            
            slopeRow.Add(paddedLinkToggle);

            slopeRow.Add(
                MakeSlopeDropdown(ref _rightSlopeDropdown, _rightSlopeDropdownProvider)
            );

            var left = MakeParameterSlider(
                ref _leftNearHeightSlider, ref _leftNearHeightSliderValue,
                "",
                delegate
                {
                    _leftNearHeightSlider.UpdateAsInt(_leftNearHeightSliderValue);
                    _tool.LeftNearHeight = (int)_leftNearHeightSlider.value;
                },
                -16, 16, 0
            );

            nearHeightRow.Add(
                MakeReversableSliderWithImage(
                    ref _leftNearHeightSlider, ref _leftNearHeightSliderValue,
                    _tool.NearTileColor, reverse: false, () =>
                    {
                        _leftNearHeightSlider.UpdateAsInt(_leftNearHeightSliderValue);
                        _tool.LeftNearHeight = (int)_leftNearHeightSlider.value;
                        UpdateSlopeSlidersOnHeightChange(_leftSlopeDropdown, _leftSlopeDropdownProvider, _leftNearHeightSlider, _leftFarHeightSlider, isOnRight: false);
                    },
                    -16, 16, 0
                )
            );

            nearHeightRow.Add(
                MakeLinkToggle(canBeToggledByOther: true, (link) =>
                {
                    _rightNearHeightSlider.SetEnabled(!link.value);
                    if (link.value)
                    {
                        _rightNearHeightSlider.value = _leftNearHeightSlider.value;
                        _leftNearHeightSlider.RegisterValueChangedCallback(UpdateLinkedMinSlider);
                    }
                    else
                    {
                        _leftNearHeightSlider.UnregisterValueChangedCallback(UpdateLinkedMinSlider);
                    }
                })
            );

            nearHeightRow.Add(
                MakeReversableSliderWithImage(
                    ref _rightNearHeightSlider, ref _rightNearHeightSliderValue,
                    _tool.NearTileColor, reverse: true, () =>
                    {
                        _rightNearHeightSlider.UpdateAsInt(_rightNearHeightSliderValue);
                        _tool.RightNearHeight = (int)_rightNearHeightSlider.value;
                        UpdateSlopeSlidersOnHeightChange(_rightSlopeDropdown, _rightSlopeDropdownProvider, _rightNearHeightSlider, _rightFarHeightSlider, isOnRight: true);
                    },
                    -16, 16, 0
                )
            );

            farHeightRow.Add(
                MakeReversableSliderWithImage(
                    ref _leftFarHeightSlider, ref _leftFarHeightSliderValue,
                    _tool.FarTileColor, reverse: false, () =>
                    {
                        _leftFarHeightSlider.UpdateAsInt(_leftFarHeightSliderValue);
                        _tool.LeftFarHeight = (int)_leftFarHeightSlider.value;
                        UpdateSlopeSlidersOnHeightChange(_leftSlopeDropdown, _leftSlopeDropdownProvider, _leftNearHeightSlider, _leftFarHeightSlider, isOnRight: false);
                    },
                    -16, 16, 3
                )
            );

            farHeightRow.Add(
                MakeLinkToggle(canBeToggledByOther: true, (link) =>
                {
                    _rightFarHeightSlider.SetEnabled(!link.value);
                    if (link.value)
                    {
                        _rightFarHeightSlider.value = _leftFarHeightSlider.value;
                        _leftFarHeightSlider.RegisterValueChangedCallback(UpdateLinkedMaxSlider);
                    }
                    else
                    {
                        _leftFarHeightSlider.UnregisterValueChangedCallback(UpdateLinkedMaxSlider);
                    }
                })
            );

            farHeightRow.Add(
                MakeReversableSliderWithImage(
                    ref _rightFarHeightSlider, ref _rightFarHeightSliderValue,
                    _tool.FarTileColor, reverse: true, () =>
                    {
                        _rightFarHeightSlider.UpdateAsInt(_rightFarHeightSliderValue);
                        _tool.RightFarHeight = (int)_rightFarHeightSlider.value;
                        UpdateSlopeSlidersOnHeightChange(_rightSlopeDropdown, _rightSlopeDropdownProvider, _rightNearHeightSlider, _rightFarHeightSlider, isOnRight: true);
                    },
                    -16, 16, 8
                )
            );

            content.Add(sideHeaderRow);
            content.Add(slopeRow);
            content.Add(nearHeightRow);
            content.Add(farHeightRow);

            // content.Add(
            //     _elementFactory.MakeToggle(
            //         "Curve debug",
            //         delegate (Toggle toggle)
            //         {
            //             _tool.CurveDebug = toggle.value;
            //         }
            //     )
            // );

            footer.Add(_elementFactory.MakeButton(_loc.T(_keyButtonApply), delegate
            {
                _tool.Apply();
            }));

            footer.Add(_elementFactory.MakeButton(_loc.T(_keyButtonClear), delegate
            {
                _tool.ResetSpline();
            }));

            footer.Add(_elementFactory.MakeButton(_loc.T(_keyButtonReset), delegate
            {
                SetDefaultOptions();
            }));

            RegisterSublinkCallbacks();
            SetDefaultOptions();
            OnModeSelect(0, SplineDrawerMode.Linear);

            return _toolPanel;
        }

        private void RegisterSublinkCallbacks()
        {
            foreach (var elem in _linkToggles)
            {
                elem.RegisterValueChangedCallback(OnAnyLinkChanged);
            }
        }

        private void OnAnyLinkChanged(ChangeEvent<bool> evt)
        {
            _linkAllToggle.SetValueWithoutNotify(_linkToggles.All(t => t.value));
        }

        private void UpdateLinkedSlopeDropdown(object sender, EventArgs e)
        {
            _rightSlopeDropdownProvider.SetValue(_leftSlopeDropdownProvider.GetValue());
            _rightSlopeDropdown.RefreshContent();
        }

        private void UpdateLinkedMinSlider(ChangeEvent<float> ev)
        {
            _rightNearHeightSlider.value = _leftNearHeightSlider.value;
        }

        private void UpdateLinkedMaxSlider(ChangeEvent<float> ev)
        {
            _rightFarHeightSlider.value = _leftFarHeightSlider.value;
        }

        private void UpdateSlopeSlidersOnHeightChange(Dropdown dropdown, SlopeDropedownProvider slopeDropedownProvider, Slider nearSlider, Slider farSlider, bool isOnRight)
        {
            bool flip = (nearSlider.value < farSlider.value) ^ isOnRight;

            if (flip != slopeDropedownProvider.Flip)
            {
                slopeDropedownProvider.Flip = flip;
                _dropdownItemsSetter.SetItems(dropdown, slopeDropedownProvider);
            }
        }


        private void SetDefaultOptions()
        {
            _radiusSlider.value = 3;
            _leftNearHeightSlider.value = 4;
            _leftFarHeightSlider.value = 0;
            _rightNearHeightSlider.value = 4;
            _rightFarHeightSlider.value = 0;

            _radiusSlider.UpdateAsInt(_radiusSliderValue);
            _leftNearHeightSlider.UpdateAsInt(_leftNearHeightSliderValue);
            _leftFarHeightSlider.UpdateAsInt(_leftFarHeightSliderValue);
            _rightNearHeightSlider.UpdateAsInt(_rightNearHeightSliderValue);
            _rightFarHeightSlider.UpdateAsInt(_rightFarHeightSliderValue);

            _linkAllToggle.value = true;

            _tool.Radius = (int)_radiusSlider.value;
            _tool.LeftNearHeight = (int)_leftNearHeightSlider.value;
            _tool.LeftFarHeight = (int)_leftFarHeightSlider.value;
            _tool.RightNearHeight = (int)_rightNearHeightSlider.value;
            _tool.RightFarHeight = (int)_rightFarHeightSlider.value;
        }


        private VisualElement MakeParameterSlider(ref Slider sliderRef, ref Label labelRef, string label, Action action, float min, float max, float initial)
        {
            VisualElement root = _elementFactory.MakeSlider(label, action, min, max, initial);
            sliderRef = root.Q("Slider") as Slider;
            labelRef = root.Q("SliderValue") as Label;

            return root;
        }

        private VisualElement MakeReversableSliderWithImage(ref Slider sliderRef, ref Label labelRef, Color imageColor, bool reverse, Action action, float min, float max, float initial)
        {
            var root = MakeParameterSlider(ref sliderRef, ref labelRef, "", action, min, max, initial);
            root.style.flexDirection = reverse ? FlexDirection.RowReverse : FlexDirection.Row;
            root.Add(MakeMarkerImage(imageColor));

            return root;
        }

        private VisualElement MakeSlopeDropdown(ref Dropdown dropdownRef, IExtendedDropdownProvider dropdownProvider, bool reverse = false)
        {
            VisualElement root = _elementFactory.MakeDropdown(dropdownProvider, reverse, displayItemText: false);
            dropdownRef = root.Q<Dropdown>();
            Utils.LogVisualTree(dropdownRef);
            return root;
        }

        private ImageToggle MakeLinkToggle(bool canBeToggledByOther, Action<ImageToggle> action, float maxWidth = 24)
        {
            var imgToggle = new ImageToggle(_textureLinked, _textureUnlinked, action, true);
            imgToggle.style.maxWidth = maxWidth;
            imgToggle.style.flexGrow = 0;
            imgToggle.style.flexShrink = 0;

            if (canBeToggledByOther)
                _linkToggles.Add(imgToggle);

            return imgToggle;
        }

        private LabelToggle MakeTextToggle(string textOn, string textOff, string textOnHoverWhileOn, string textOnHoverWhileOff, Action<LabelToggle> onValueChanged, bool defaultState = false)
        {
            LabelToggle element = new(textOn, textOff, textOnHoverWhileOn, textOnHoverWhileOff, onValueChanged, defaultState);
            var template = _elementFactory.MakeLabel("");
            element.ApplyClassesFrom(template);
            return element;
        }

        private Image MakeMarkerImage(Color color)
        {
            Image image = new()
            {
                image = _textureMarker,
                tintColor = color
            };
            return image;
        }

        private void OnModeSelect(int index, SplineDrawerMode mode)
        {
            OnModeSelect(_modeButtons[index], mode);
        }
        private void OnModeSelect(Button button, SplineDrawerMode mode)
        {
            foreach (var child in _modeButtons)
            {
                child.SetEnabled(child != button);
            }
            _tool.Mode = mode;
        }
    }
}