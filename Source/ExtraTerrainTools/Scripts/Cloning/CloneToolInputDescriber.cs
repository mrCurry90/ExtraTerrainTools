using System.Collections.Generic;
using System.Collections.ObjectModel;
using static TerrainTools.Cloning.In;

namespace TerrainTools.Cloning
{
    public class CloneToolInputDescriber
    {
        #region Key Constants         
        // Defined as Blueprints > Keybindings
        // Consolidated adjustment keys. Use Shift modifier to toggle between Position (no modifier)
        // and Size (Shift held).
        public const string KEY_ADJUST_LEFT = "ExtraTerrainTools.SelectionAdjustLeft";
        public const string KEY_ADJUST_RIGHT = "ExtraTerrainTools.SelectionAdjustRight";
        public const string KEY_ADJUST_FORWARD = "ExtraTerrainTools.SelectionAdjustForward";
        public const string KEY_ADJUST_BACKWARD = "ExtraTerrainTools.SelectionAdjustBack";
        public const string KEY_ADJUST_UP = "ExtraTerrainTools.SelectionAdjustUp";
        public const string KEY_ADJUST_DOWN = "ExtraTerrainTools.SelectionAdjustDown";
        public const string KEY_ADJUST_MODIFIER = "ExtraTerrainTools.SelectionAdjustModifier";
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
        public const string GROUP_SIZE_TRIM = "TerrainTools.CloneTool.InputGroup.SizeTrim";
        public const string GROUP_RESET = "TerrainTools.CloneTool.InputGroup.Reset";
        #endregion

        #region Action Loc Keys
        // Defined in Localization
        public const string ACTION_LEFT_CLICK = "TerrainTools.CloneTool.InputDesciption.LeftClick";
        public const string ACTION_RIGHT_CLICK = "TerrainTools.CloneTool.InputDesciption.RightClick";
        public const string ACTION_CLICK_N_DRAG = "TerrainTools.CloneTool.InputDesciption.ClickAndDrag";
        public const string ACTION_MOUSE_SCROLL = "TerrainTools.CloneTool.InputDesciption.MouseScroll";
        public const string ACTION_HOLD = "TerrainTools.CloneTool.InputDesciption.Hold";
        public const string ACTION_HOLD_AND = "TerrainTools.CloneTool.InputDesciption.HoldAnd";

        #endregion

        private Dictionary<CloneToolPhase, List<CloneToolInputGroup>> _phaseInputsMap;

        public ReadOnlyDictionary<CloneToolPhase, List<CloneToolInputGroup>> AllInputGroups { get; private set; }

        public CloneToolInputDescriber()
        {
            _phaseInputsMap = new()
            {
                {
                    CloneToolPhase.Start, new List<CloneToolInputGroup>
                    {
                        CloneToolInputGroup.DescribedAs(GROUP_SELECT)
                            .OrderedBy(1)
                            .PerformedBy(TextInput(ACTION_CLICK_N_DRAG)),
                        CloneToolInputGroup.DescribedAs(GROUP_CANCEL)
                            .OrderedBy(2)
                            .PerformedBy(TextInput(ACTION_RIGHT_CLICK)),
                    }
                },
                {
                    CloneToolPhase.Base, new List<CloneToolInputGroup>
                    {
                        CloneToolInputGroup.DescribedAs(GROUP_SELECT)
                            .OrderedBy(1)
                            .PerformedBy(TextInput(ACTION_CLICK_N_DRAG)),
                        CloneToolInputGroup.DescribedAs(GROUP_CANCEL)
                            .OrderedBy(2)
                            .PerformedBy(TextInput(ACTION_RIGHT_CLICK)),
                    }
                },
                {
                    CloneToolPhase.Height, new List<CloneToolInputGroup>
                    {
                        CloneToolInputGroup.DescribedAs(GROUP_HEIGHT_SELECT)
                            .OrderedBy(1)
                            .PerformedBy(
                                TextInput(ACTION_MOUSE_SCROLL)
                            ),
                        CloneToolInputGroup.DescribedAs(GROUP_SIZE_TRIM)
                            .OrderedBy(2)
                            .PerformedBy(
                                KeyInput(KEY_ADJUST_LEFT),
                                KeyInput(KEY_ADJUST_RIGHT),
                                KeyInput(KEY_ADJUST_FORWARD),
                                KeyInput(KEY_ADJUST_BACKWARD),
                                KeyInput(KEY_ADJUST_UP),
                                KeyInput(KEY_ADJUST_DOWN)
                            ),
                        CloneToolInputGroup.DescribedAs(GROUP_CONFIRM)
                            .OrderedBy(3)
                            .PerformedBy(TextInput(ACTION_LEFT_CLICK)),
                        CloneToolInputGroup.DescribedAs(GROUP_CANCEL)
                            .OrderedBy(4)
                            .PerformedBy(TextInput(ACTION_RIGHT_CLICK)),
                    }
                },
                {
                    CloneToolPhase.MoveApply, new List<CloneToolInputGroup>
                    {
                        CloneToolInputGroup.DescribedAs(GROUP_MOVE_SELECTION)
                            .OrderedBy(1)
                            .PerformedBy(TextInput(ACTION_CLICK_N_DRAG)),
                        CloneToolInputGroup.DescribedAs(GROUP_CUT)
                            .OrderedBy(2)
                            .PerformedBy(KeyInput(KEY_CUT)),
                        CloneToolInputGroup.DescribedAs(GROUP_COPY)
                            .OrderedBy(3)
                            .PerformedBy(KeyInput(KEY_COPY)),
                        CloneToolInputGroup.DescribedAs(GROUP_PASTE)
                            .OrderedBy(4)
                            .PerformedBy(KeyInput(KEY_PASTE)),
                        CloneToolInputGroup.DescribedAs(GROUP_ROTATE)
                            .OrderedBy(5)
                            .PerformedBy(KeyInput(KEY_ROTATE_CW), KeyInput(KEY_ROTATE_CCW)),
                        CloneToolInputGroup.DescribedAs(GROUP_FLIP)
                            .OrderedBy(6)
                            .PerformedBy(KeyInput(KEY_FLIP)),
                        CloneToolInputGroup.DescribedAs(GROUP_POSITION_TRIM)
                            .OrderedBy(7)
                            .PerformedBy(
                                KeyInput(KEY_ADJUST_LEFT),
                                KeyInput(KEY_ADJUST_RIGHT),
                                KeyInput(KEY_ADJUST_FORWARD),
                                KeyInput(KEY_ADJUST_BACKWARD),
                                KeyInput(KEY_ADJUST_UP),
                                KeyInput(KEY_ADJUST_DOWN)
                            ),
                        CloneToolInputGroup.DescribedAs(GROUP_SIZE_TRIM)
                            .OrderedBy(8)
                            .PerformedBy(FormattedInput(ACTION_HOLD_AND, Keybind(KEY_ADJUST_MODIFIER), Text(GROUP_POSITION_TRIM))),
                        CloneToolInputGroup.DescribedAs(GROUP_RESET)
                            .OrderedBy(9)
                            .PerformedBy(FormattedInput(ACTION_HOLD, Keybind(KEY_RESET))),
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