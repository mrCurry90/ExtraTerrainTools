using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrainTools.Cloning
{
    public class CloneToolInputDescriber
    {
        #region Key Constants         
        // Defined as Blueprints > Keybindings
        public const string KEY_HEIGHT_UP = "ExtraTerrainTools.SelectionHeightUp";
        public const string KEY_HEIGHT_DOWN = "ExtraTerrainTools.SelectionHeightDown";
        public const string KEY_POS_FORWARD = "ExtraTerrainTools.SelectionPosForward";
        public const string KEY_POS_BACKWARD = "ExtraTerrainTools.SelectionPosBack";
        public const string KEY_POS_LEFT = "ExtraTerrainTools.SelectionPosLeft";
        public const string KEY_POS_RIGHT = "ExtraTerrainTools.SelectionPosRight";
        public const string KEY_POS_UP = "ExtraTerrainTools.SelectionPosUp";
        public const string KEY_POS_DOWN = "ExtraTerrainTools.SelectionPosDown";
        public const string KEY_COPY = "ExtraTerrainTools.SelectionCopy";
        public const string KEY_CUT = "ExtraTerrainTools.SelectionCut";
        public const string KEY_PASTE = "ExtraTerrainTools.SelectionPaste";
        public const string KEY_ROTATE_CW = "ExtraTerrainTools.SelectionRotateCW";
        public const string KEY_ROTATE_CCW = "ExtraTerrainTools.SelectionRotateCCW";
        public const string KEY_FLIP = "ExtraTerrainTools.SelectionFlip";
        public const string KEY_RESET = "ExtraTerrainTools.SelectionReset";
        #endregion

        #region Group Loc Keys
        // Defined in Localization
        public const string GROUP_SELECT = "TerrainTools.CloneTool.InputGroup.Select";
        public const string GROUP_CANCEL = "TerrainTools.CloneTool.InputGroup.Cancel";
        public const string GROUP_HEIGHT_SELECT = "TerrainTools.CloneTool.InputGroup.HeightSelect";
        public const string GROUP_CONFIRM = "TerrainTools.CloneTool.InputGroup.Confirm";
        public const string GROUP_MOVE_SELECTION = "TerrainTools.CloneTool.InputGroup.MoveSelection";
        public const string GROUP_CUT = "TerrainTools.CloneTool.InputGroup.Cut";
        public const string GROUP_COPY = "TerrainTools.CloneTool.InputGroup.Copy";
        public const string GROUP_PASTE = "TerrainTools.CloneTool.InputGroup.Paste";
        public const string GROUP_ROTATE = "TerrainTools.CloneTool.InputGroup.Rotate";
        public const string GROUP_FLIP = "TerrainTools.CloneTool.InputGroup.Flip";
        public const string GROUP_POSITION_TRIM = "TerrainTools.CloneTool.InputGroup.PositionTrim";
        public const string GROUP_HEIGHT_TRIM = "TerrainTools.CloneTool.InputGroup.HeightTrim";
        public const string GROUP_RESET = "TerrainTools.CloneTool.InputGroup.Reset";
        #endregion

        #region Action Loc Keys
        // Defined in Localization
        public const string ACTION_LEFT_CLICK = "TerrainTools.CloneTool.InputDesciption.LeftClick";
        public const string ACTION_RIGHT_CLICK = "TerrainTools.CloneTool.InputDesciption.RightClick";
        public const string ACTION_CLICK_N_DRAG = "TerrainTools.CloneTool.InputDesciption.ClickAndDrag";
        public const string ACTION_MOUSE_SCROLL = "TerrainTools.CloneTool.InputDesciption.MouseScroll";
        public const string ACTION_HOLD = "TerrainTools.CloneTool.InputDesciption.Hold";

        #endregion

        private Dictionary<CloneToolPhase, List<CloneToolInputGroup>> _phaseInputsMap;

        public ReadOnlyDictionary<CloneToolPhase, List<CloneToolInputGroup>> AllInputGroups { get; private set; }

        public CloneToolInputDescriber()
        {
            var builder = new CloneToolInputGroup.Builder();
            _phaseInputsMap = new()
            {
                {
                    CloneToolPhase.Start, new List<CloneToolInputGroup>
                    {
                        builder
                            .SetDescription(GROUP_SELECT).SetOrder(1)
                            .AddDescriptionInput(ACTION_CLICK_N_DRAG)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_CANCEL).SetOrder(2)
                            .AddDescriptionInput(ACTION_RIGHT_CLICK)
                            .BuildAndClear(),
                    }
                },
                {
                    CloneToolPhase.Base, new List<CloneToolInputGroup>
                    {
                        builder.SetDescription(GROUP_SELECT).SetOrder(1)
                            .AddDescriptionInput(ACTION_CLICK_N_DRAG)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_CANCEL).SetOrder(2)
                            .AddDescriptionInput(ACTION_RIGHT_CLICK)
                            .BuildAndClear(),
                    }
                },
                {
                    CloneToolPhase.Height, new List<CloneToolInputGroup>
                    {
                        builder.SetDescription(GROUP_HEIGHT_SELECT).SetOrder(1)
                            .AddDescriptionInput(ACTION_MOUSE_SCROLL)
                            .AddKeybindInput(KEY_HEIGHT_UP)
                            .AddKeybindInput(KEY_HEIGHT_DOWN)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_CONFIRM).SetOrder(2)
                            .AddDescriptionInput(ACTION_LEFT_CLICK)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_CANCEL).SetOrder(3)
                            .AddDescriptionInput(ACTION_RIGHT_CLICK)
                            .BuildAndClear(),
                    }
                },
                {
                    CloneToolPhase.MoveApply, new List<CloneToolInputGroup>
                    {
                        builder.SetDescription(GROUP_MOVE_SELECTION).SetOrder(1)
                            .AddDescriptionInput(ACTION_CLICK_N_DRAG)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_CUT).SetOrder(2)
                            .AddKeybindInput(KEY_CUT)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_COPY).SetOrder(3)
                            .AddKeybindInput(KEY_COPY)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_PASTE).SetOrder(4)
                            .AddKeybindInput(KEY_PASTE)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_ROTATE).SetOrder(5)
                            .AddKeybindInput(KEY_ROTATE_CW)
                            .AddKeybindInput(KEY_ROTATE_CCW)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_FLIP).SetOrder(6)
                            .AddKeybindInput(KEY_FLIP)
                            .AddDescriptionInput("<-- WIP for objects, use carefully")
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_HEIGHT_TRIM).SetOrder(7)
                            .AddKeybindInput(KEY_HEIGHT_UP)
                            .AddKeybindInput(KEY_HEIGHT_DOWN)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_POSITION_TRIM).SetOrder(8)
                            .AddKeybindInput(KEY_POS_LEFT)
                            .AddKeybindInput(KEY_POS_RIGHT)
                            .AddKeybindInput(KEY_POS_FORWARD)
                            .AddKeybindInput(KEY_POS_BACKWARD)
                            .AddKeybindInput(KEY_POS_UP)
                            .AddKeybindInput(KEY_POS_DOWN)
                            .BuildAndClear(),
                        builder.SetDescription(GROUP_RESET).SetOrder(9)
                            .AddInput(ACTION_HOLD, KEY_RESET)
                            .BuildAndClear(),
                    }
                }
            };

            AllInputGroups = new(_phaseInputsMap);
        }

        public IEnumerable<CloneToolInputGroup> GetPhaseInputGroups(CloneToolPhase phase)
        {
            if (_phaseInputsMap.TryGetValue(phase, out var inputGroups))
                return inputGroups;

            return null;
        }
    }
}