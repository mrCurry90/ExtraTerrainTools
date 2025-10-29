using System;
using System.Collections.Generic;
using System.Linq;
using TerrainTools.EditorHistory;
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
using Timberborn.ToolSystem;
using Timberborn.WaterSourceSystem;
using UnityEngine;
using DescriptionBuilder = Timberborn.ToolSystem.ToolDescription.Builder;
using Object = UnityEngine.Object;
using InputDescriber = TerrainTools.Cloning.CloneToolInputDescriber;

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
        private readonly TerrainToolsAssetService _assetService;
        private readonly CameraService _cameraService;
        private readonly TerrainPicker _terrainPicker;
        private readonly IBlockService _blockService;
        private readonly ITerrainService _terrainService;
        private readonly EditorHistoryService _historyService;
        private readonly TerrainToolsManipulationService _manipulationService;
        private readonly KeyBindingRegistry _keyBindingRegistry;
        private readonly MarkerDrawerFactory _markerDrawerFactory;

        // Colors for visualization        
        // Selection box
        private readonly Color _selectModeFrameColor = new(0f, 0.8f, 1f, 1f);
        private readonly Color _adjustModeFrameColor = new(0.2f, 1f, 0.2f, 1f);
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
        private readonly string _selectionBoxPrefabPath = "SelectionBox";
        private SelectionBox _selectionBoxPrefab;
        private SelectionBox _selectionBox;
        private int _selectionBoxHeight = 0;

        // Selection buffer
        private Selection _selectionBuffer = new();
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

        public CloneTool(
            InputService inputService,
            TerrainToolsAssetService assetService,
            CameraService cameraService,
            TerrainPicker terrainPicker,
            IBlockService blockService,
            ITerrainService terrainService,
            EditorHistoryService historyService,
            KeyBindingRegistry keyBindingRegistry,
            TerrainToolsManipulationService manipulationService,
            MarkerDrawerFactory markerDrawerFactory,
            ILoc loc
        ) : base(loc)
        {
            _inputService = inputService;
            _assetService = assetService;
            _cameraService = cameraService;
            _terrainPicker = terrainPicker;
            _blockService = blockService;
            _terrainService = terrainService;
            _historyService = historyService;
            _keyBindingRegistry = keyBindingRegistry;
            _manipulationService = manipulationService;
            _markerDrawerFactory = markerDrawerFactory;
        }

        public void Load()
        {
            _toolDescription = new DescriptionBuilder(_loc.T(_keyToolTitle))
                .AddPrioritizedSection(_loc.T(_keyToolDescription))
                .Build();

            if (_selectionBoxPrefab == null)
            {
                _selectionBoxPrefab = _assetService.Fetch<SelectionBox>(_selectionBoxPrefabPath, TerrainToolsAssetService.Folder.Prefabs);
            }

            _smallBlockDrawer = _markerDrawerFactory.CreateSmallBlockTileDrawer();
            _largeBlockDrawer = _markerDrawerFactory.CreateLargeBlockTileDrawer();
        }
        public override ToolDescription Description()
        {
            return _toolDescription;
        }

        public override void Enter()
        {
            _inputService.AddInputProcessor(this);

            _selectionBox ??= Instantiate(_selectionBoxPrefab).GetComponent<SelectionBox>();
            // Utils.Log($"Enter _toolPhase = {_toolPhase}");
            if (_toolPhase < CloneToolPhase.MoveApply)
            {
                // Utils.Log($"Selectionbox inactive");
                _selectionBox.Active = false;
            }
            else
            {
                // Utils.Log($"Selection box active");
                _selectionBox.Active = true;
                ApplyAdjustmentsToBox();
            }
        }

        public override void Exit()
        {
            _inputService.RemoveInputProcessor(this);

            // Utils.Log($"Exit _toolPhase = {_toolPhase}");

            // Reset the tool if no confirmed selection exists
            if (_toolPhase < CloneToolPhase.MoveApply)
            {
                // Utils.Log("Resetting tool");
                ResetTool();
            }
            else
            {
                _selectionBox.Active = false;
            }
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
                DrawBufferPreviews();
            }

            if (_selectedPoints.Count != _lastFramePointCount)
            {
                _lastFramePointCount = _selectedPoints.Count;
            }
            return interrupt;
        }

        private void ResetTool()
        {
            _selectionBuffer?.Clear();
            _selectedPoints.Clear();
            _selectionBox.Active = false;
            _selectionBox.Color = _selectModeFrameColor;
            _selectionBoxHeight = 0;
            _dragging = false;
            _dragOffset = Vector3Int.zero;

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
            var adjust = _inputService.MouseZoom;
            if (adjust == 0f)
            {
                if (_inputService.IsKeyDown(InputDescriber.KEY_HEIGHT_UP) || _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_HEIGHT_UP))
                    adjust++;

                if (_inputService.IsKeyDown(InputDescriber.KEY_HEIGHT_DOWN) || _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_HEIGHT_DOWN))
                    adjust--;

                // If key is held at this stage, we consider input handled to cover the gap between key down and long hold.
                inputHandled = _inputService.IsKeyHeld(InputDescriber.KEY_HEIGHT_UP) || _inputService.IsKeyHeld(InputDescriber.KEY_HEIGHT_DOWN);
            }

            if (adjust != 0f)
            {
                if (adjust > 0f)
                {
                    _selectionBoxHeight++;
                }
                else
                {
                    _selectionBoxHeight--;
                }

                var p3 = _selectedPoints[1] + new Vector3Int(0, 0, _selectionBoxHeight);
                _selectionBox.Set(GridCenterToWorld(_selectedPoints[0]), GridCenterToWorld(p3));
                inputHandled = true;
            }

            if (_inputService.MainMouseButtonDown)
            {
                var p3 = _selectedPoints[1] + new Vector3Int(0, 0, _selectionBoxHeight);
                _selectedPoints.Add(p3);
                _selectionBox.Color = _adjustModeFrameColor;
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
            // Adjusting height with keyboard
            else if (_inputService.IsKeyHeld(InputDescriber.KEY_HEIGHT_UP) || _inputService.IsKeyHeld(InputDescriber.KEY_HEIGHT_DOWN))
            {
                var adjust = 0;
                if (_inputService.IsKeyDown(InputDescriber.KEY_HEIGHT_UP) || _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_HEIGHT_UP))
                    adjust++;

                if (_inputService.IsKeyDown(InputDescriber.KEY_HEIGHT_DOWN) || _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_HEIGHT_DOWN))
                    adjust--;

                if (adjust != 0)
                {
                    _selectedPoints[^1] += new Vector3Int(0, 0, adjust);
                    selectionAdjusted = true;
                }
                inputHandled = true;
            }
            // Adjusting position with keyboard
            else if (
                _inputService.IsKeyHeld(InputDescriber.KEY_POS_FORWARD) || _inputService.IsKeyHeld(InputDescriber.KEY_POS_BACKWARD) ||
                _inputService.IsKeyHeld(InputDescriber.KEY_POS_LEFT) || _inputService.IsKeyHeld(InputDescriber.KEY_POS_RIGHT) ||
                _inputService.IsKeyHeld(InputDescriber.KEY_POS_UP) || _inputService.IsKeyHeld(InputDescriber.KEY_POS_DOWN)
            )
            {
                var keyAdjust = Vector3Int.zero;
                if (_inputService.IsKeyDown(InputDescriber.KEY_POS_FORWARD) || _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_POS_FORWARD))
                    keyAdjust.y += 1;
                if (_inputService.IsKeyDown(InputDescriber.KEY_POS_BACKWARD) || _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_POS_BACKWARD))
                    keyAdjust.y -= 1;
                if (_inputService.IsKeyDown(InputDescriber.KEY_POS_LEFT) || _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_POS_LEFT))
                    keyAdjust.x -= 1;
                if (_inputService.IsKeyDown(InputDescriber.KEY_POS_RIGHT) || _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_POS_RIGHT))
                    keyAdjust.x += 1;
                if (_inputService.IsKeyDown(InputDescriber.KEY_POS_UP) || _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_POS_UP))
                    keyAdjust.z += 1;
                if (_inputService.IsKeyDown(InputDescriber.KEY_POS_DOWN) || _keyBindingRegistry.IsLongHeld(InputDescriber.KEY_POS_DOWN))
                    keyAdjust.z -= 1;

                if (keyAdjust != Vector3Int.zero)
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

                    for (int i = 0; i < _selectedPoints.Count; i++)
                    {
                        _selectedPoints[i] += offset;
                    }

                    selectionAdjusted = true;
                }
                inputHandled = true;
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
            if (_inputService.IsKeyDown(InputDescriber.KEY_COPY))
            {
                Copy();
                return true;
            }
            else if (_inputService.IsKeyDown(InputDescriber.KEY_CUT))
            {
                _historyService.BatchStart();
                Cut();
                return true;
            }
            else if (_inputService.IsKeyDown(InputDescriber.KEY_PASTE))
            {
                _historyService.BatchStart();
                Paste();
                return true;
            }
            else if (_inputService.IsKeyDown(InputDescriber.KEY_ROTATE_CW))
            {
                Rotate(clockwise: true);
                return true;
            }
            else if (_inputService.IsKeyDown(InputDescriber.KEY_ROTATE_CCW))
            {
                Rotate(clockwise: false);
                return true;
            }
            else if (_inputService.IsKeyDown(InputDescriber.KEY_FLIP))
            {
                Flip();
                return true;
            }

            if (_inputService.IsKeyUp(InputDescriber.KEY_CUT) || _inputService.IsKeyUp(InputDescriber.KEY_PASTE))
            {
                _historyService.BatchStop();
            }

            return false;
        }

        private void Cut()
        {
            Copy();

            // Perform cut
            // _historyService.BatchStart();
            foreach (var objectData in _selectionBuffer.GetObjects())
            {
                var obj = _manipulationService.GetFirstObjectAt(objectData.PrefabName, objectData.Coordinates);
                if (obj != null)
                    _manipulationService.DeleteObject(obj);
            }

            foreach (var (coord, isTerrain) in _selectionBuffer.GetTerrain())
            {
                if (isTerrain && _manipulationService.CanRemoveTerrain(coord))
                    _terrainService.UnsetTerrain(coord);

            }
            // _historyService.BatchStop();
        }

        private void Copy()
        {
            _selectionBuffer.Scan(_terrainService, _blockService, _selectedPoints.First(), _selectedPoints.Last(), includeStacked: IncludeStackedObjects, includeAir: IncludeAirBlocks);
            SelectedContentChanged?.Invoke();
        }

        private void Paste()
        {
            foreach (var (coord, isTerrain) in _selectionBuffer.GetTerrain())
            {
                // Add terrain back in, ignoring restrictions
                if (isTerrain)
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
                _manipulationService.PlaceObject(objectData.PrefabName, new(objectData.Coordinates, objectData.Orientation, objectData.FlipMode), objectData.Growth);
            }
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

        private void DrawBufferPreviews()
        {
            DrawTerrainPreview();
            DrawObjectPreviews();
        }

        private void DrawTerrainPreview()
        {
            foreach (var (coord, setToTerrain) in _selectionBuffer.GetTerrain())
            {
                if (_terrainService.Underground(coord))
                {
                    _largeBlockDrawer.DrawAtCoordinates(coord, verticalOffset: PREVIEW_VERT_OFFSET, setToTerrain ? _terrainAddColor : _terrainRemColor);
                }
                else if (setToTerrain)
                {
                    _largeBlockDrawer.DrawAtCoordinates(coord, verticalOffset: PREVIEW_VERT_OFFSET, setToTerrain ? _terrainAddColor : _terrainRemColor);
                }
            }
        }

        private void DrawObjectPreviews()
        {
            foreach (var obj in _selectionBuffer.GetObjects())
            {
                var spec = _manipulationService.GetBlockObjectSpec(obj.PrefabName);
                if (spec != null)
                {
                    var blocks = spec.GetBlocks();
                    int width = blocks.Size.x % 2 == 0 ? 2 : 1;
                    foreach (var relCoord in blocks.GetOccupiedCoordinates())
                    {
                        var oriented = obj.Orientation.Transform(obj.FlipMode.Transform(relCoord, width));
                        // Utils.Log($"relCoord: {relCoord}");
                        // Utils.Log($"oriented: {oriented}");
                        _smallBlockDrawer.DrawAtCoordinates(
                            obj.Coordinates + oriented,
                            verticalOffset: PREVIEW_VERT_OFFSET,
                            GetObjectColor(obj.PrefabName)
                        );
                    }
                }
            }
        }

        private Color GetObjectColor(string prefabName)
        {
            var service = _manipulationService;
            var prefab = _manipulationService.GetPrefabSpec(prefabName);
            if (service.GetPrefabComponent<RuinSpec>(prefab))
                return _objectRuinColor;
            if (service.GetPrefabComponent<GrowableSpec>(prefab))
                return _objectGrowableColor;
            if (service.GetPrefabComponent<WaterSourceSpec>(prefab))
            {
                var contaminationSpec = service.GetPrefabComponent<WaterSourceContaminationSpec>(prefab);
                return contaminationSpec.DefaultContamination > 0f ? _objectBadWaterColor : _objectWaterColor;
            }

            return _objectBaseColor;
        }

        private bool HasRayHitSelection(Ray ray)
        {
            ray = CoordinateSystem.WorldToGrid(ray);
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
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
            _selectionBuffer?.UpdatePosition(_selectedPoints.First(), _selectedPoints.Last());
            _selectionBox.Set(
                GridCenterToWorld(
                    _selectionBuffer.Transform(_selectedPoints.First())
                ),
                GridCenterToWorld(
                    _selectionBuffer.Transform(_selectedPoints.Last())
                )
            );
        }

        // Gameobject wrappers for cleaner code
        private static T Instantiate<T>(T original) where T : Object
        {
            return Object.Instantiate<T>(original);
        }

        private static void DestroyObject(Component obj)
        {
            Object.Destroy(obj.gameObject);
        }
    }
}