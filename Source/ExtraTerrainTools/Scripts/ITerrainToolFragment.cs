using System;
using Timberborn.CoreUI;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using Timberborn.ToolPanelSystem;
using Timberborn.ToolSystem;
using UnityEngine.UIElements;

namespace TerrainTools
{
    /*************************************************************
     TerrainTool class
        Extends Tool-class with additional methods to get 
        the desired tool panel configuration 
    **************************************************************/
    public abstract class ITerrainToolFragment : IToolFragment
    {
        private Type _toolType;
        private int _baseOrder = 60;
        protected readonly ILoc _loc;
        protected VisualElement Root { get; private set; }


        public int Order { get { return _baseOrder + ToolOrder; } }

        protected int ToolOrder { private get; set; }

        private ITerrainToolFragment() { }

        public ITerrainToolFragment(EventBus eventBus, Type toolType, ILoc loc)
        {
            _toolType = toolType;
            _loc = loc;

            eventBus.Register(this);
        }

        public VisualElement InitializeFragment()
        {
            Root = BuildToolPanelContent();
            Root.ToggleDisplayStyle(visible: false);
            return Root;
        }

        public abstract VisualElement BuildToolPanelContent();

        [OnEvent]
        public void OnToolEntered(ToolEnteredEvent toolEnteredEvent)
        {
            if (toolEnteredEvent.Tool.GetType() == _toolType)
            {
                OnToolEnteredDerived(toolEnteredEvent);
                Root.ToggleDisplayStyle(visible: true);
            }
        }


        [OnEvent]
        public void OnToolExited(ToolExitedEvent toolExitedEvent)
        {
            if (toolExitedEvent.Tool.GetType() == _toolType)
            {
                OnToolExitedDerived(toolExitedEvent);
                Root.ToggleDisplayStyle(visible: false);
            }
        }

        protected virtual void OnToolEnteredDerived(ToolEnteredEvent toolEnteredEvent) { }

        protected virtual void OnToolExitedDerived(ToolExitedEvent toolExitedEvent) { }
    }
}