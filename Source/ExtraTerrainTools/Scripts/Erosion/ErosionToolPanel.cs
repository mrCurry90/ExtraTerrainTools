using UnityEngine.UIElements;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using Timberborn.UndoSystem;
using System.Collections.Generic;

namespace TerrainTools.Erosion
{
    public class ErosionToolPanel : TerrainToolFragment
    {
        public readonly ErosionTool _tool;
        private readonly TerrainToolPanelFactory _panelFactory;
        private readonly IUndoRegistry _undoRegistry;

        private Dictionary<string, float> _parameters = new()
            {
                { "Rain Amount", 6f },
                { "Evaporation Rate", 0.01f },
                { "Erosion Rate", 0.15f },
                { "Deposition Rate", 0.1f },
                { "Sediment Capacity Constant", 2.0f },
                { "Simulation Steps", 50f }
            };

        public ErosionToolPanel(ErosionTool tool, TerrainToolPanelFactory panelFactory, IUndoRegistry undoRegistry, EventBus eventBus, ILoc loc) : base(eventBus, typeof(ErosionTool), loc)
        {
            _tool = tool;
            _panelFactory = panelFactory;
            _undoRegistry = undoRegistry;
        }

        public override VisualElement BuildToolPanelContent()
        {
            var panel = _panelFactory.MakeToolPanel();

            panel.Add(_panelFactory.MakeLabel("Parameters"));
            var paramContainer = _panelFactory.MakeContainer(FlexDirection.Column, Align.FlexStart, Justify.FlexStart);
            paramContainer.style.width = new Length(50, LengthUnit.Percent);
            panel.Add(paramContainer);
            foreach (var param in _parameters)
            {
                paramContainer.Add(_panelFactory.MakeLabel(param.Key + ":"));
                var paramField = _panelFactory.MakeTextField((field) =>
                {
                    if (float.TryParse(field.value, out float parsedValue))
                    {
                        _parameters[param.Key] = parsedValue;
                    }
                    else
                    {
                        field.value = _parameters[param.Key].ToString(); // reset to last valid value
                    }
                });
                paramField.value = param.Value.ToString();
                paramContainer.Add(paramField);
            }

            var button = _panelFactory.MakeTextButton("Apply", () =>
            {
                _tool.Apply(
                    _parameters["Rain Amount"],
                    _parameters["Evaporation Rate"],
                    _parameters["Erosion Rate"],
                    _parameters["Deposition Rate"],
                    _parameters["Sediment Capacity Constant"],
                    (int)_parameters["Simulation Steps"]
                );
                _undoRegistry.CommitStack();
            });

            panel.Add(button);

            return panel;
        }
    }
}