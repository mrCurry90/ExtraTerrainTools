using System.Collections.Generic;
using System.Linq;
using System.Text;
using Timberborn.Common;
using Timberborn.KeyBindingSystemUI;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using UnityEngine.UIElements;
using UnityEngine;
using Phase = TerrainTools.Cloning.CloneToolPhase;

namespace TerrainTools.Cloning
{
    public class CloneToolPanel : TerrainToolFragment
    {
        private readonly TerrainToolPanelFactory _elementFactory;
        private readonly CloneTool _cloneTool;
        private readonly CloneToolInputDescriber _inputDescriber;
        private readonly InputBindingDescriber _inputBindingDescriber;
        private VisualElement _toolPanel;
        private Label _toolTipLabel;
        private VisualElement _keysTipContainer;
        private Toggle _includeAirToggle;
        // private Toggle _includeStackedToggle;
        private Label _footerStatsLabel;

        private readonly string _keyTipNoText = "TerrainTools.CloneTool.Error.NoText";
        private readonly string _keyToggleIncludeAir = "TerrainTools.CloneTool.Option.IncludeAirBlocks";
        // private readonly string _keyToggleIncludeStacked = "TerrainTools.CloneTool.Option.IncludeStackedBlocks";
        private readonly Dictionary<Phase, string> _toolTipTextKeys = new()
        {
            { Phase.Start, "TerrainTools.CloneTool.ToolTip.Start" },
            { Phase.Base, "TerrainTools.CloneTool.ToolTip.Base" },
            { Phase.Height, "TerrainTools.CloneTool.ToolTip.Height" },
            { Phase.MoveApply, "TerrainTools.CloneTool.ToolTip.MoveApply" }
        };

        public CloneToolPanel(TerrainToolPanelFactory elementFactory, CloneTool cloneTool, InputBindingDescriber inputBindingDescriber, EventBus eventBus, ILoc loc) : base(eventBus, typeof(CloneTool), loc)
        {
            _elementFactory = elementFactory;
            _cloneTool = cloneTool;
            _inputBindingDescriber = inputBindingDescriber;
            _inputDescriber = new();
            ToolOrder = 5;
        }

        public override VisualElement BuildToolPanelContent()
        {
            // Build structure
            var header = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceBetween);
            var content = _elementFactory.MakeDescriptionContainer(FlexDirection.Column, Align.Stretch);
            var footer = _elementFactory.MakeContainer(FlexDirection.Row, Align.Center, Justify.SpaceBetween);

            header.style.paddingBottom = footer.style.paddingTop = new Length(5f, LengthUnit.Pixel);

            // Header
            _toolTipLabel = _elementFactory.MakeLabel(_loc.T(_keyTipNoText), TextAnchor.MiddleCenter);

            var headerLeft = new VisualElement();
            var headerRight = _elementFactory.MakeMinimizerButton(content);

            headerRight.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
            {
                headerLeft.style.maxHeight = headerRight.resolvedStyle.height;
                headerLeft.style.minHeight = headerRight.resolvedStyle.height;
                headerLeft.style.minWidth = headerRight.resolvedStyle.width;
                headerLeft.style.maxWidth = headerRight.resolvedStyle.width;
            });

            header.Add(headerLeft);
            header.Add(_toolTipLabel);
            header.Add(headerRight);

            // Content
            foreach (var (phase, groups) in _inputDescriber.AllInputGroups)
            {
                content.Add(MakePhaseContainer(phase, groups));
            }

            _keysTipContainer = content;

            // Footer
            _includeAirToggle = _elementFactory.MakeToggle(
                _loc.T(_keyToggleIncludeAir),
                (t) => _cloneTool.IncludeAirBlocks = t.value,
                _cloneTool.IncludeAirBlocks
            );

            // Removed for now since it has no effect, TODO: Investigate why
            // _includeStackedToggle = _elementFactory.MakeToggle(
            //     _loc.T(_keyToggleIncludeStacked),
            //     (t) => _cloneTool.IncludeStackedObjects = t.value,
            //     _cloneTool.IncludeStackedObjects
            // );

            _footerStatsLabel = _elementFactory.MakeLabel(GetFooterStats());

            footer.Add(_includeAirToggle);
            // footer.Add(_includeStackedToggle);
            footer.Add(_footerStatsLabel);

            // Assemble container
            _toolPanel = _elementFactory.MakeToolPanel(FlexDirection.Column, Align.Stretch);

            _toolPanel.Add(header);
            _toolPanel.Add(content);
            _toolPanel.Add(footer);

            UpdateToolPanel(_cloneTool.Phase);

            return _toolPanel;
        }

        private VisualElement MakePhaseContainer(Phase phase, IEnumerable<CloneToolInputGroup> groups)
        {
            var phaseContainer = _elementFactory.MakeContainer(FlexDirection.Column, Align.Stretch, Justify.Center);
            phaseContainer.userData = phase;
            phaseContainer.style.display = DisplayStyle.None; // Start not rendered

            var groupPadding = new Length(5f, LengthUnit.Pixel);
            var labelLength = new Length(100f, LengthUnit.Pixel);
            var inputsLength = new Length(66f, LengthUnit.Percent);

            foreach (var group in groups.OrderBy(g => g.Order))
            {
                // Group row
                var groupContainer = _elementFactory.MakeContainer(FlexDirection.Row, Align.FlexStart, Justify.FlexStart);
                groupContainer.style.paddingTop = groupPadding;
                phaseContainer.Add(groupContainer);

                // Group label
                var labelContainer = _elementFactory.MakeContainer(FlexDirection.Column, Align.FlexEnd, Justify.FlexStart);
                labelContainer.style.minWidth = labelLength;
                labelContainer.style.maxHeight = labelLength;
                labelContainer.style.flexWrap = Wrap.Wrap;
                labelContainer.style.flexGrow = 0;

                labelContainer.Add(_elementFactory.MakeLabel(
                    $"{Localize(group.NameKey)}:"
                ));
                groupContainer.Add(labelContainer);

                // Input
                var inputsContainer = _elementFactory.MakeContainer(FlexDirection.Row, Align.FlexStart, Justify.FlexStart);
                inputsContainer.style.flexWrap = Wrap.Wrap;
                inputsContainer.style.flexGrow = 3;
                inputsContainer.style.minWidth = inputsLength;
                inputsContainer.style.maxWidth = inputsLength;

                AddInputsToContainer(group.Inputs, inputsContainer);

                groupContainer.Add(inputsContainer);

            }

            phaseContainer.Children().Last().style.paddingBottom = groupPadding;

            return phaseContainer;
        }

        private void AddInputsToContainer(CloneToolInput[] inputs, VisualElement container)
        {
            var builder = new StringBuilder();
            int last = inputs.Length - 1;
            for (int i = 0; i < inputs.Length; i++)
            {
                var input = inputs[i];

                if (input.HasDescription())
                {
                    builder.Append(Localize(input.DescriptionKey));
                }

                if (input.HasDescription() && input.HasKeybind())
                {
                    builder.Append(" ");
                }

                if (input.HasKeybind())
                    builder.Append(_inputBindingDescriber.GetInputBindingText(input.KeybindId));

                if (i < last)
                    builder.Append(",");

                container.Add(
                    _elementFactory.MakeLabel(
                        builder.ToStringWithoutNewLineEndAndClean()
                    )
                );
            }
        }

        protected override void OnToolEnteredDerived(ToolEnteredEvent toolEnteredEvent)
        {
            _cloneTool.PhaseChanged += OnPhaseChanged;
            _cloneTool.SelectedContentChanged += OnSelectedContentChanged;
        }

        protected override void OnToolExitedDerived(ToolExitedEvent toolExitedEvent)
        {
            _cloneTool.PhaseChanged -= OnPhaseChanged;
            _cloneTool.SelectedContentChanged -= OnSelectedContentChanged;
        }

        private void OnPhaseChanged(Phase prevPhase, Phase newPhase)
        {
            UpdateToolPanel(newPhase);
        }

        private void OnSelectedContentChanged()
        {
            _footerStatsLabel.text = GetFooterStats();
        }

        private string GetFooterStats()
        {
            return $"Objects: {_cloneTool.ObjectCount} Terrain: {_cloneTool.TerrainCount}";
        }

        private void UpdateToolPanel(Phase phase)
        {
            // Update options availibility
            _includeAirToggle.SetEnabled(phase == Phase.MoveApply);
            // _includeStackedToggle.SetEnabled(phase == Phase.MoveApply);

            // Update texts
            _toolTipLabel.text = $"<b>{GetPhaseToolTip(phase)}</b>";
            foreach (var child in _keysTipContainer.Children())
            {
                child.style.display = (Phase)child.userData == phase ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private string Localize(string text)
        {
            var localized = _loc.T(text);
            return localized.Equals(text) ? _loc.T(_keyTipNoText) : localized;
        }

        private string GetPhaseToolTip(Phase phase)
        {
            return Localize(_toolTipTextKeys.GetValueOrDefault(phase, _keyTipNoText));
        }
    }
}