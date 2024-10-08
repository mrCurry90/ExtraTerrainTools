using System;
using Timberborn.SingletonSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace TerrainTools.PathPainterTool
{
    public class PathPainterToolPanel : ITerrainToolFragment
    {
        private readonly TerrainToolPanelFactory _elementFactory;
        private readonly PathPainterTool _tool;
        private VisualElement _toolPanel;

        public PathPainterToolPanel(TerrainToolPanelFactory toolPanelFactory, EventBus eventBus, PathPainterTool tool)
        : base(eventBus, typeof(PathPainterTool))
        {
            _elementFactory = toolPanelFactory;
            _tool = tool;
            ToolOrder = 20;
        }

        public override VisualElement BuildToolPanelContent()
        {
            _toolPanel = _elementFactory.MakeTemplatePanel( FlexDirection.Column, Align.Stretch );
            var header  = _elementFactory.MakeContainer( FlexDirection.Column, Align.Stretch );
            var content = _elementFactory.MakeContainer( FlexDirection.Column, Align.Stretch );
            var footer = _elementFactory.MakeContainer( FlexDirection.Column, Align.Stretch );
            
            _toolPanel.Add(header);
            _toolPanel.Add(content);
            _toolPanel.Add(footer);


            content.Add(_elementFactory.MakeLabel("<b>Work In Progress</b>", TextAnchor.MiddleCenter));            
 
            var footerRow = _elementFactory.MakeContainer( FlexDirection.Row, Align.Center, Justify.SpaceBetween );
            footer.Add(footerRow);

            return _toolPanel;
        }
    }
}