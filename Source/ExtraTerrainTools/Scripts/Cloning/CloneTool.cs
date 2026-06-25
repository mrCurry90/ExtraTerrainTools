using DescriptionBuilder = Timberborn.ToolSystemUI.ToolDescription.Builder;
using InputDescriber = TerrainTools.Cloning.CloneToolInputDescriber;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Linq;
using System;
using Timberborn.BlockSystem;
using Timberborn.CameraSystem;
using Timberborn.Coordinates;
using Timberborn.GridTraversing;
using Timberborn.Growing;
using Timberborn.InputSystem;
using Timberborn.KeyBindingSystem;
using Timberborn.Localization;
using Timberborn.Rendering;
using Timberborn.Ruins;
using Timberborn.SingletonSystem;
using Timberborn.TerrainQueryingSystem;
using Timberborn.TerrainSystem;
using Timberborn.ToolSystemUI;
using Timberborn.UndoSystem;
using Timberborn.WaterSourceSystem;
using UnityEngine;
using Timberborn.Stockpiles;
using Timberborn.BlueprintSystem;

namespace TerrainTools.Cloning
{
    public class CloneTool : TerrainTool, IInputProcessor, ILoadableSingleton
    {
        /// <summary>
        /// Occurs when the phase of the <see cref="CloneTool"/> changes.
        /// </summary>
        /// <remarks>
        /// The event provides the previous phase and the new phase as parameters,
        /// allowing subscribers to react to transitions between the different tool states in
        /// <see cref="CloneToolPhase"/>.
        /// </remarks>
        /// <param name="previousPhase">The previous <see cref="CloneToolPhase"/> before the change.</param>
        /// <param name="newPhase">The new <see cref="CloneToolPhase"/> after the change.</param>
        public event Action<CloneToolPhase, CloneToolPhase> PhaseChanged;
        public event Action SelectedContentChanged;

        // Tool description
        public override string Icon { get; } = "CloneToolIcon";
        private readonly string _keyToolTitle = "TerrainTools.CloneTool.Title";
        private readonly string _keyToolDescription = "TerrainTools.CloneTool.Description";
        private ToolDescription _toolDescription;

        // Settings
        public bool IncludeStackedObjects { get; set; } = true;
        public bool IncludeAirBlocks { get; set; } = false;

        // Dependencies
        private readonly InputService _inputService;
        private readonly CameraService _cameraService;
        private readonly TerrainPicker _terrainPicker;
        private readonly IBlockService _blockService;
        private readonly ITerrainService _terrainService;
        private readonly IUndoRegistry _undoRegistry;
        private readonly TerrainToolsManipulationService _manipulationService;
        private readonly KeyBindingRegistry _keyBindingRegistry;
        private readonly MarkerDrawerFactory _markerDrawerFactory;
        private readonly ISpecService _specService;

        // Colors for visualization        
        // Selection box
        private readonly Color _selectModeFrameColor = new(0f, 0.8f, 1f, 1f);
        private readonly Color _adjustPositionFrameColor = new(0.2f, 1f, 0.2f, 1f);
        private readonly Color _adjustSizeFrameColor = new(1f, 0.8f, 0.2f, 1f);

        // Content previews
        // Terrain 
        // private readonly Color _terrainAddColor = new(0.360f, 0.243f, 0.164f, 1f); // Brown
        // private readonly Color _terrainAddColor = new(0.356f, 0.290f, 0.223f, 1f); // Dark grey brown
        private readonly Color _terrainAddColor = new(0.223f, 0.223f, 0.223f, 1f); // Dark grey
        private readonly Color _terrainRemColor = new(1f, 0.2f, 0.2f, 1f); // Red

        // Objects
        // private readonly Color _objectBaseColor = new(0.305f, 0.207f, 0.141f, 1f); // Dark brown
        // private readonly Color _objectBaseColor = new(0.350f, 0.350f, 0.223f, 1f); // Dark grey
        private readonly Color _objectBaseColor = new(0.223f, 0.223f, 0.223f, 1f); // Dark grey

        // private readonly Color _objectRuinColor = new(0.531f, 0.543f, 0.551f, 1f); // Gray
        private readonly Color _objectRuinColor = new(0.717f, 0.254f, 0.054f, 1f); // Rusty

        private readonly Color _objectGrowableColor = new(0f, 0.43f, 0.2f, 1f); // Forest green

        private readonly Color _objectWaterColor = new(0f, 0.367f, 0.719f, 1f); // Ocean blue

        private readonly Color _objectBadWaterColor = new(0.5f, 0.02f, 0f, 1f); // Dark red

        // Private fields
        // Selection box
        private readonly Vector3 _gridCenterOffset = new(0.5f, 0.5f, 0.5f);
        private SelectionBoxSpec _selectionBoxSpec;
        private SelectionBox _selectionBox;
        private int _selectionBoxHeight = 0;

        // Selection buffer
        private Selection _selectionBuffer;
        public int ObjectCount { get => _selectionBuffer == null ? 0 : _selectionBuffer.ObjectCount; }
        public int TerrainCount { get => _selectionBuffer == null ? 0 : _selectionBuffer.TerrainCount; }

        // Input handling and control
        private CloneToolPhase _toolPhase = CloneToolPhase.Start;
        public CloneToolPhase Phase { get => _toolPhase; }
        private List<Vector3Int> _selectedPoints = new();
        private int _lastFramePointCount = 0;
        private Vector3Int _dragOffset;
        private bool _dragging;

        // Preview marking
        private MeshDrawer _smallBlockDrawer;
        private MeshDrawer _largeBlockDrawer;
        private const float PREVIEW_VERT_OFFSET = 0.02f;

        // Keyboard input throttling (for IsLongHeld repeat rate)
        private float _adjustFrameTimer = 0f;
        private float _adjustTimerNextTick = 0f;
        private const float MAX_REPEAT_DELAY = 1f / 10f;
        private const float MIN_REPEAT_DELAY = 1f / 60f;
        private const float ACCELERATION_TIME = 2f;    // Time to reach minimum repeat delay with smoothing
        public CloneTool(
            InputService inputService,
            CameraService cameraService,
            TerrainPicker terrainPicker,
            IBlockService blockService,
            ITerrainService terrainService,
            IUndoRegistry undoRegistry,
            KeyBindingRegistry keyBindingRegistry,
            TerrainToolsManipulationService manipulationService,
            MarkerDrawerFactory markerDrawerFactory,
            ISpecService specService,
            ILoc loc
        ) : base(loc)
        {
            _inputService = inputService;
            _cameraService = cameraService;
            _terrainPicker = terrainPicker;
            _blockService = blockService;
            _terrainService = terrainService;
            _undoRegistry = undoRegistry;
            _keyBindingRegistry = keyBindingRegistry;
            _manipulationService = manipulationService;
            _markerDrawerFactory = markerDrawerFactory;
            _specService = specService;
            _selectionBuffer = new(_terrainService, _blockService, _manipulationService);
        }

        public void Load()
        {
            _toolDescription = new DescriptionBuilder(_loc.T(_keyToolTitle))
                .AddPrioritizedSection(_loc.T(_keyToolDescription))
                .Build();

            _selectionBoxSpec = _specService.GetSingleSpec<SelectionBoxSpec>() ?? throw new MissingReferenceException("Failed to load SelectionBoxSpec");

            _smallBlockDrawer = _markerDrawerFactory.CreateSmallBlockTileDrawer();
            _largeBlockDrawer = _markerDrawerFactory.CreateLargeBlockTileDrawer();
        }
        public override ToolDescription DescribeTool()
        {
            return _toolDescription;
        }

        public override void Enter()
        {
            _inputService.AddInputProcessor(this);

            _selectionBox ??= Instantiate(_selectionBoxSpec.BoxPrefab.Asset).AddComponent<SelectionBox>();
            _selectionBox.Initialize(_selectionBoxSpec.BaseColor);

            if (_toolPhase < CloneToolPhase.MoveApply)
            {
                _selectionBox.Active = false;
            }
            else
            {
                _selectionBox.Active = true;
                ApplyAdjustmentsToBox();
            }
        }

        public override void Exit()
        {
            _inputService.RemoveInputProcessor(this);


            // Reset the tool if no confirmed selection exists
            if (_toolPhase < CloneToolPhase.MoveApply)
            {
                ResetTool();
            }
            else
            {
                _selectionBox.Active = false;
            }

            // Commit changes to editor history
            _undoRegistry.CommitStack();
        }
        public bool ProcessInput()
        {
            bool interrupt = false;
            if (_inputService.Cancel && _selectedPoints.Count > 0 && _toolPhase < CloneToolPhase.MoveApply)
            {
                ResetTool();
                interrupt = true;
            }
            else if (_inputService.IsKeyLongHeld(InputDescriber.KEY_RESET) && _toolPhase == CloneToolPhase.MoveApply)
            {
                ResetTool();
                interrupt = true;
            }
            else if (!_inputService.MouseOverUI)
            {
                Ray ray = _cameraService.ScreenPointToRayInGridSpace(_inputService.MousePosition);
                interrupt = _toolPhase switch
                {
                    < CloneToolPhase.MoveApply => HandleSelect(ray),
                    CloneToolPhase.MoveApply => HandleMoveApply(ray),
                    _ => false,
                };
            }

            if (_toolPhase == CloneToolPhase.MoveApply)
            {
                DrawTerrainPreview();
                DrawObjectPreviews();
            }

            if (_selectedPoints.Count != _lastFramePointCount)
            {
                _lastFramePointCount = _selectedPoints.Count;
            }
            return interrupt;
        }

        private void ResetTool()
        {
            _selectionBuffer.Clear();
            _selectedPoints.Clear();
            _selectionBox.Active = false;
            _selectionBox.Color = _selectModeFrameColor;
            _selectionBoxHeight = 0;
            _dragging = false;
            _dragOffset = Vector3Int.zero;
            _adjustFrameTimer = 0f;
            _adjustTimerNextTick = 0f;

            UpdateToolPhase();
        }

        private bool HandleSelect(Ray ray)
        {
            return _selectedPoints.Count switch
            {
                0 => HandleSelectNoPoints(ray),
                1 => HandleSelectOnePoint(ray),
                2 => HandleSelectTwoPoints(ray),
                _ => false,
            };
        }

        private bool HandleSelectNoPoints(Ray ray)
        {
            if (HasRayHitTerrain(ray, out var there))
            {
                _selectionBox.Active = true;
                _selectionBox.Position = GridCenterToWorld(there);
                if (_selectionBox.Scale != Vector3.one)
                    _selectionBox.Scale = Vector3.one;
                if (_inputService.MainMouseButtonDown)
                {
                    _selectedPoints.Add(there);
                    UpdateToolPhase();
                    return true;
                }
            }
            else
            {
                _selectionBox.Active = false;
            }
            return false;
        }

        private bool HandleSelectOnePoint(Ray ray)
        {
            _selectionBox.Active = true;

            Vector3Int prev = _selectedPoints.Last();
            if (HasRayHitPlane(ray, prev.z, out var there))
            {
                _selectionBox.Set(GridCenterToWorld(prev), GridCenterToWorld(there));

                if (!_inputService.MainMouseButtonHeld) // Add second point on release
                {
                    _selectedPoints.Add(there);
                    UpdateToolPhase();
                    return true;
                }
            }
            return false;
        }

        private bool HandleSelectTwoPoints(Ray ray)
        {
            _selectionBox.Active = true;

            if (_selectedPoints.Count < 2) throw new ArgumentOutOfRangeException("_selectedPoints.Count", "Handler for two points called with < 2 selected points.");

            bool inputHandled = false;
            var adjust = Vector3Int.zero;
            adjust.z = _inputService.MouseZoom > 0f ? 1 : (_inputService.MouseZoom < 0f ? -1 : 0);
            if (adjust == Vector3Int.zero)
            {
                if (AnyKeyboardAdjustmentHeld())
                {
                    adjust = GetKeyboardAdjustments();
                    // If key is held at this stage, we consider input handled to cover the gap between key down and long hold.
                    inputHandled = true;
                }
            }

            if (adjust != Vector3Int.zero)
            {
                var p2 = _selectedPoints[1];
                p2.x += adjust.x;
                p2.y += adjust.y;
                _selectedPoints[1] = p2;
                _selectionBoxHeight += adjust.z;

                var p3 = p2 + new Vector3Int(0, 0, _selectionBoxHeight);
                _selectionBox.Set(GridCenterToWorld(_selectedPoints[0]), GridCenterToWorld(p3));
                inputHandled = true;
            }

            if (_inputService.MainMouseButtonDown)
            {
                var p3 = _selectedPoints[1] + new Vector3Int(0, 0, _selectionBoxHeight);
                _selectedPoints.Add(p3);
                _selectionBox.Color = _adjustPositionFrameColor;
                UpdateToolPhase();

                inputHandled = true;
            }

            return inputHandled;
        }

        // TODO: Handle clicking on a corner to adjust?
        private bool HandleSelectionAdjustments(Ray ray)
        {
            _selectionBox.Active = true;

            if (_selectedPoints.Count != _lastFramePointCount)
            {
                ApplyAdjustmentsToBox(); // TODO: Why do we need this?
            }

            bool inputHandled = false;
            bool selectionAdjusted = false;

            // Dragging with mouse
            if (_inputService.MainMouseButtonUp)
                _dragging = false;

            if (_inputService.MainMouseButtonHeld)
            {
                _selectionBox.Color = _adjustPositionFrameColor;
                if (HasRayHitTerrain(ray, out var there) || HasRayHitPlane(ray, _selectedPoints.First().z, out there))
                {
                    if (_inputService.MainMouseButtonDown && HasRayHitSelection(ray))
                    {
                        _dragOffset = there;
                        _dragging = true;
                    }

                    if (_dragging)
                    {
                        // TODO: When dragging the behaviour could be better
                        Vector3Int offset = there - _dragOffset;
                        float zoom = _inputService.MouseZoom;
                        int adjust = zoom > 0f ? 1 : (zoom < 0f ? -1 : 0);
                        if (adjust != 0)
                        {
                            inputHandled = true;
                            offset.z += adjust;
                        }

                        if (offset != Vector3Int.zero)
                        {
                            for (int i = 0; i < _selectedPoints.Count; i++)
                            {
                                _selectedPoints[i] += offset;
                            }
                            _dragOffset = there;
                            selectionAdjusted = true;
                        }
                    }
                }
            }

            // Adjust Position (move all points) and Size (move far corner only).
            else if (AnyKeyboardAdjustmentHeld())
            {
                bool modifySize = _inputService.IsKeyHeld(InputDescriber.KEY_ADJUST_MODIFIER);
                if (modifySize)
                {
                    _selectionBox.Color = _adjustSizeFrameColor;
                    _smallBlockDrawer.DrawAtCoordinates(
                        _selectedPoints[^1],
                        verticalOffset: PREVIEW_VERT_OFFSET,
                        _adjustSizeFrameColor
                    );
                }
                else
                {
                    _selectionBox.Color = _adjustPositionFrameColor;
                }


                Vector3Int keyAdjust = GetKeyboardAdjustments();

                if (keyAdjust != Vector3Int.zero)
                {
                    Vector3Int offset = CameraRelativeToWorld(keyAdjust);
                    if (modifySize)
                    {
                        // Size adjustment: move far corner only (last point)
                        _selectedPoints[^1] += offset;
                    }
                    else
                    {
                        // Position adjustment: move all points, rotated by camera angle for X/Y
                        for (int i = 0; i < _selectedPoints.Count; i++)
                        {
                            _selectedPoints[i] += offset;
                        }
                    }

                    selectionAdjusted = true;
                }
                inputHandled = true;
            }
            else if (_inputService.IsKeyHeld(InputDescriber.KEY_ADJUST_MODIFIER))
            {
                _selectionBox.Color = _adjustSizeFrameColor;
                _smallBlockDrawer.DrawAtCoordinates(
                    _selectedPoints[^1],
                    verticalOffset: PREVIEW_VERT_OFFSET,
                    _adjustSizeFrameColor
                );
            }
            else
            {
                _selectionBox.Color = _adjustPositionFrameColor;
            }

            if (selectionAdjusted)
            {
                ApplyAdjustmentsToBox();
            }

            return inputHandled;
        }

        private bool HandleMoveApply(Ray ray)
        {
            bool inputHandled = HandleSelectionAdjustments(ray);

            if (!inputHandled)
            {
                inputHandled = HandleShortcuts();
            }

            return inputHandled;
        }

        private bool HandleShortcuts()
        {
            if (_inputService.IsKeyDown(InputDescriber.KEY_COPY)) { Copy(); return true; }
            else if (_inputService.IsKeyDown(InputDescriber.KEY_CUT)) { Cut(); return true; }
            else if (_inputService.IsKeyDown(InputDescriber.KEY_PASTE)) { Paste(); return true; }
            else if (_inputService.IsKeyDown(InputDescriber.KEY_ROTATE_CW)) { Rotate(clockwise: true); return true; }
            else if (_inputService.IsKeyDown(InputDescriber.KEY_ROTATE_CCW)) { Rotate(clockwise: false); return true; }
            else if (_inputService.IsKeyDown(InputDescriber.KEY_FLIP)) { Flip(); return true; }

            return false;
        }

        private void Cut()
        {
            Copy();

            // Perform cut
            foreach (var objectData in _selectionBuffer.GetObjects())
            {
                var obj = _manipulationService.GetFirstObjectAt(objectData.TemplateName, objectData.Coordinates);
                if (obj != null)
                    _manipulationService.DeleteObject(obj);
            }

            foreach (var (coord, isTerrain) in _selectionBuffer.GetTerrain())
            {
                if (isTerrain && _manipulationService.CanRemoveTerrain(coord))
                    _terrainService.UnsetTerrain(coord);

            }

            // Commit changes to editor history
            _undoRegistry.CommitStack();
        }

        private void Copy()
        {
            _selectionBuffer.Scan(_selectedPoints.First(), _selectedPoints.Last(), includeStacked: IncludeStackedObjects, includeAir: IncludeAirBlocks);
            SelectedContentChanged?.Invoke();
        }

        private void Paste()
        {
            foreach (var (coord, shouldAddTerrain) in _selectionBuffer.GetTerrain())
            {
                // Add terrain back in, ignoring restrictions
                if (shouldAddTerrain)
                    _terrainService.SetTerrain(coord);
                else if (IncludeAirBlocks)
                {
                    _terrainService.UnsetTerrain(coord);
                    // If airblocks included we also delete any existing objects in that space
                    var existingObjects = _blockService.GetObjectsAt(coord);
                    for (int i = 0; i < existingObjects.Count; i++)
                    {
                        _manipulationService.DeleteObject(existingObjects[i]);
                    }
                }
            }

            // Paste stored objects
            foreach (var objectData in _selectionBuffer.GetObjects().OrderBy(o => o.Coordinates.z))
            {
                try
                {
                    // If block object requires full depth (i.e. watersource or underground ruin) pasting incorrectly can cause crash.
                    // TODO: Add validation
                    _manipulationService.PlaceObject(
                        objectData.TemplateName,
                        new(objectData.Coordinates, objectData.Orientation, objectData.FlipMode),
                        (placed) =>
                        {
                            if (objectData.Growth >= 0 && placed.TryGetComponent<Growable>(out var growable))
                            {
                                growable.IncreaseGrowthProgress(objectData.Growth);
                            }

                            if (objectData.GoodId != "" && placed.TryGetComponent<Stockpile>(out var stockpile))
                            {
                                _manipulationService.Set(stockpile, objectData.GoodId, objectData.GoodAmount);
                            }
                        }
                    );
                }
                catch (InvalidOperationException)
                {
                    Utils.Log(objectData.ToString());
                    throw;
                }
            }

            // Commit changes to editor history
            _undoRegistry.CommitStack();
        }

        private void Flip()
        {
            _selectionBuffer.Flip();
            ApplyAdjustmentsToBox();
        }

        private void Rotate(bool clockwise)
        {
            _selectionBuffer.Rotate(clockwise);
            ApplyAdjustmentsToBox();
        }

        private void UpdateToolPhase()
        {
            var previousPhase = _toolPhase;
            _toolPhase = _selectedPoints.Count switch
            {
                0 => CloneToolPhase.Start,
                1 => CloneToolPhase.Base,
                2 => CloneToolPhase.Height,
                3 => CloneToolPhase.MoveApply,
                _ => CloneToolPhase.Start
            };

            if (_toolPhase != previousPhase)
            {
                PhaseChanged?.Invoke(previousPhase, _toolPhase);
            }
        }

        private void DrawTerrainPreview()
        {
            foreach (var (coord, shouldAddTerrain) in _selectionBuffer.GetTerrain())
            {
                if (_terrainService.Underground(coord) || shouldAddTerrain)
                {
                    _largeBlockDrawer.DrawAtCoordinates(coord, verticalOffset: PREVIEW_VERT_OFFSET, shouldAddTerrain ? _terrainAddColor : _terrainRemColor);
                }
            }
        }

        private void DrawObjectPreviews()
        {
            foreach (var obj in _selectionBuffer.GetObjects())
            {
                var spec = _manipulationService.GetBlockObjectSpec(obj.TemplateName);
                if (spec != null)
                {
                    var blocks = spec.GetBlocks();
                    foreach (var relCoord in blocks.GetOccupiedCoordinates())
                    {
                        var oriented = blocks.Transform(relCoord, new(obj.Coordinates, obj.Orientation, obj.FlipMode));
                        _smallBlockDrawer.DrawAtCoordinates(
                            oriented,
                            // obj.Coordinates + oriented,
                            verticalOffset: PREVIEW_VERT_OFFSET,
                            GetObjectColor(obj.TemplateName)
                        );
                    }
                }
            }
        }

        private bool AnyKeyboardAdjustmentHeld()
        {
            return _inputService.IsKeyHeld(InputDescriber.KEY_ADJUST_FORWARD) || _inputService.IsKeyHeld(InputDescriber.KEY_ADJUST_BACKWARD)
                || _inputService.IsKeyHeld(InputDescriber.KEY_ADJUST_LEFT) || _inputService.IsKeyHeld(InputDescriber.KEY_ADJUST_RIGHT)
                || _inputService.IsKeyHeld(InputDescriber.KEY_ADJUST_UP) || _inputService.IsKeyHeld(InputDescriber.KEY_ADJUST_DOWN);
        }

        private Vector3Int GetKeyboardAdjustments()
        {
            var adjust = Vector3Int.zero;

            // Immediate key down adjustments
            if (_inputService.IsKeyDown(InputDescriber.KEY_ADJUST_FORWARD))
                adjust.y++;
            if (_inputService.IsKeyDown(InputDescriber.KEY_ADJUST_BACKWARD))
                adjust.y--;
            if (_inputService.IsKeyDown(InputDescriber.KEY_ADJUST_LEFT))
                adjust.x--;
            if (_inputService.IsKeyDown(InputDescriber.KEY_ADJUST_RIGHT))
                adjust.x++;
            if (_inputService.IsKeyDown(InputDescriber.KEY_ADJUST_UP))
                adjust.z++;
            if (_inputService.IsKeyDown(InputDescriber.KEY_ADJUST_DOWN))
                adjust.z--;

            // Throttled repeats for long-held keys
            if (adjust == Vector3Int.zero)
            {
                bool anyAdjustHeld =
                    _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_FORWARD) ||
                    _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_BACKWARD) ||
                    _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_LEFT) ||
                    _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_RIGHT) ||
                    _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_UP) ||
                    _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_DOWN);

                if (ShouldApplyThrottledAdjustment(anyAdjustHeld))
                {
                    if (_keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_FORWARD))
                        adjust.y++;
                    if (_keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_BACKWARD))
                        adjust.y--;
                    if (_keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_LEFT))
                        adjust.x--;
                    if (_keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_RIGHT))
                        adjust.x++;
                    if (_keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_UP))
                        adjust.z++;
                    if (_keyBindingRegistry.IsLongHeld(InputDescriber.KEY_ADJUST_DOWN))
                        adjust.z--;
                }
            }

            return adjust;
        }

        private bool ShouldApplyThrottledAdjustment(bool isCurrentlyHeld)
        {
            if (!isCurrentlyHeld)
            {
                _adjustFrameTimer = 0f;
                _adjustTimerNextTick = 0f;
                return false;
            }

            // Key is held, apply throttling
            _adjustFrameTimer += Time.unscaledDeltaTime;

            if (_adjustFrameTimer >= _adjustTimerNextTick)
            {
                float t = Mathf.Clamp01(_adjustFrameTimer / ACCELERATION_TIME);
                float repeatDelay = Mathf.SmoothStep(MAX_REPEAT_DELAY, MIN_REPEAT_DELAY, t);
                _adjustTimerNextTick = _adjustFrameTimer + repeatDelay;
                return true;
            }

            return false;
        }

        private Vector3Int CameraRelativeToWorld(Vector3Int keyAdjust)
        {

            var angle = _cameraService.HorizontalAngle % 360;
            if (angle < 0) angle += 360;

            Vector3Int offset = angle switch
            {
                >= 315 or < 45 => new Vector3Int(keyAdjust.x, keyAdjust.y, keyAdjust.z),    // Facing "forward"
                >= 45 and < 135 => new Vector3Int(keyAdjust.y, -keyAdjust.x, keyAdjust.z),   // Facing "right"
                >= 135 and < 225 => new Vector3Int(-keyAdjust.x, -keyAdjust.y, keyAdjust.z), // Facing "back"
                >= 225 and < 315 => new Vector3Int(-keyAdjust.y, keyAdjust.x, keyAdjust.z),   // Facing "left"
                _ => Vector3Int.zero
            };
            return offset;
        }

        private Color GetObjectColor(string prefabName)
        {
            var service = _manipulationService;
            var template = _manipulationService.GetTemplateSpec(prefabName);
            if (service.GetSpec<RuinSpec>(template) != null)
                return _objectRuinColor;
            if (service.GetSpec<GrowableSpec>(template) != null)
                return _objectGrowableColor;
            if (service.GetSpec<WaterSourceSpec>(template) != null)
            {
                var contaminationSpec = service.GetSpec<WaterSourceContaminationSpec>(template);
                return contaminationSpec.DefaultContamination > 0f ? _objectBadWaterColor : _objectWaterColor;
            }

            return _objectBaseColor;
        }

        private bool HasRayHitSelection(Ray ray)
        {
            ray = CoordinateSystem.WorldToGrid(ray);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, Physics.IgnoreRaycastLayer))
            {
                if (hitInfo.collider != null)
                {
                    if (hitInfo.collider.TryGetComponent<SelectionBox>(out var box))
                    {
                        return box == _selectionBox;
                    }
                }
            }

            return false;
        }

        private bool HasRayHitTerrain(Ray ray, out Vector3Int where)
        {
            TraversedCoordinates? traversedCoordinates = _terrainPicker.PickTerrainCoordinates(ray);
            if (traversedCoordinates.HasValue)
            {
                TraversedCoordinates valueOrDefault = traversedCoordinates.GetValueOrDefault();
                where = valueOrDefault.Coordinates + valueOrDefault.Face;
                return true;
            }

            where = Vector3Int.zero;
            return false;
        }

        private bool HasRayHitPlane(Ray ray, int referenceHeight, out Vector3Int where)
        {
            TraversedCoordinates? coordinates = _terrainPicker.FindCoordinatesOnLevelInMap(ray, referenceHeight);
            if (coordinates.HasValue)
            {
                where = coordinates.GetValueOrDefault().Coordinates;
                return true;
            }

            where = Vector3Int.zero;
            return false;
        }

        private Vector3 GridCenterToWorld(Vector3Int coordinate)
        {
            Vector3 p = coordinate + _gridCenterOffset;
            return p.XZY();
        }

        private void ApplyAdjustmentsToBox()
        {
            _selectionBuffer.UpdatePosition(_selectedPoints.First(), _selectedPoints.Last());
            _selectionBox.Set(
                GridCenterToWorld(_selectionBuffer.Start),
                GridCenterToWorld(_selectionBuffer.End)
            );
        }

        // Gameobject wrappers for cleaner code
        private static T Instantiate<T>(T original) where T : Object
        {
            return Object.Instantiate<T>(original);
        }
    }
}