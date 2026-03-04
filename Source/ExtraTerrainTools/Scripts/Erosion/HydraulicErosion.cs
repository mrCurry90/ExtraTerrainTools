using Timberborn.TerrainSystem;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace TerrainTools.Erosion
{
    public class HydraulicErosion
    {
        private readonly ITerrainService _terrainService;
        private readonly TerrainToolsManipulationService _manipulationService;

        /// <summary>
        /// Creates a new <see cref="HydraulicErosion"/> instance.
        /// </summary>
        /// <param name="terrainService">Provides access to terrain height and terrain size.</param>
        /// <param name="manipulationService">Service used to apply computed terrain adjustments.</param>
        public HydraulicErosion(
            ITerrainService terrainService, TerrainToolsManipulationService manipulationService)
        {
            _terrainService = terrainService;
            _manipulationService = manipulationService;
        }

        // Integer terrain
        int[,] _originalHeight; // original terrain heights, read-only
        float[,] _height; // internal state

        // Float simulation layers
        float[,] _water;
        float[,] _sediment;

        // Parameters
        int _simulationSteps;
        float _rainAmount;
        float _evaporation;
        float _erosionRate;
        float _depositionRate;
        float _capacityConstant;
        float _minSlope = 1f; // integer terrain step, should bo at least 1 to prevent excessive erosion on flat areas

        RectInt _area;

        bool _isValid = false;

        /// <summary>
        /// Initializes internal buffers and sets parameters for the erosion simulation.
        /// </summary>
        /// <param name="area">Rectangular area (in cells) to simulate.</param>
        /// <param name="simulationSteps">Number of simulation iterations to perform.</param>
        /// <param name="rainAmount">Initial water added per cell.</param>
        /// <param name="evaporation">Fraction of water lost per step (0..1).</param>
        /// <param name="erosionRate">Rate at which terrain is eroded when under capacity.</param>
        /// <param name="despositionRate">Rate at which sediment is deposited when over capacity.</param>
        /// <param name="capacity">Capacity constant used to compute sediment carrying capacity.</param>
        public void Initialize(
            RectInt area,
            int simulationSteps, float rainAmount,
            float evaporation, float erosionRate,
            float despositionRate, float capacity
        )
        {
            _area = area;
            _simulationSteps = simulationSteps;
            _rainAmount = rainAmount;
            _evaporation = evaporation;
            _erosionRate = erosionRate;
            _depositionRate = despositionRate;
            _capacityConstant = capacity;

            var size = _terrainService.Size;

            _originalHeight = new int[size.x, size.y];
            _height = new float[size.x, size.y];
            _water = new float[size.x, size.y];
            _sediment = new float[size.x, size.y];
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                {
                    _originalHeight[x, y] = _terrainService.CellHeight(new(x, y));
                    _height[x, y] = _originalHeight[x, y];
                    _water[x, y] = 0f;
                    _sediment[x, y] = 0f;
                }

            _isValid = false;
        }

        /// <summary>
        /// Runs the erosion simulation as a Unity coroutine; yields once per simulation step.
        /// </summary>
        /// <returns>IEnumerator for use with StartCoroutine; does nothing if not initialized.</returns>
        public IEnumerator SimulateCoroutine()
        {
            Validate();
            if (!_isValid)
                yield break;

            AddWater();

            for (int i = 0; i < _simulationSteps; i++)
            {
                Erode();
                yield return null;
            }
        }

        /// <summary>
        /// Runs the erosion simulation synchronously (blocking).
        /// </summary>
        /// <returns>True if the simulation ran; false if instance was not initialized.</returns>
        public bool Simulate()
        {
            Validate();
            if (!_isValid)
                return false;

            AddWater();

            for (int i = 0; i < _simulationSteps; i++)
            {
                Erode();
            }

            return true;
        }

        /// <summary>
        /// Applies the computed terrain adjustments to the terrain using the manipulation service.
        /// </summary>
        /// <remarks>Only non-zero adjustments inside the simulated area are applied.</remarks>
        public void ApplyResults()
        {
            if (!_isValid)
                return;

            ClampToTerrain(_area, out Vector2Int origin, out Vector2Int size);

            for (int x = origin.x; x < size.x - 1; x++)
            {
                for (int y = origin.y; y < size.y - 1; y++)
                {
                    var z1 = _originalHeight[x, y];
                    var z2 = Mathf.RoundToInt(_height[x, y]);

                    var zDiff = z2 - z1;
                    if (zDiff != 0)
                    {
                        Utils.Log($"Applied adjustment {zDiff} at ({x}, {y}), original height {_originalHeight[x, y]}, final height {z2}");
                        _manipulationService.AdjustTerrain(new(x, y, z1), zDiff);
                    }
                }
            }
        }

        private void Validate()
        {
            if (_area.size.magnitude <= 0)
                return;

            _isValid = true;
        }

        private void ClampToTerrain(RectInt area, out Vector2Int origin, out Vector2Int size)
        {
            origin = area.min;
            size = area.size;

            // Ensure a 1-cell border to avoid out-of-bounds during erosion
            if (origin.x < 1) origin.x = 1;
            if (origin.y < 1) origin.y = 1;
            if (origin.x + size.x >= _terrainService.Size.x) size.x = _terrainService.Size.x - origin.x - 1;
            if (origin.y + size.y >= _terrainService.Size.y) size.y = _terrainService.Size.y - origin.y - 1;
        }

        private void AddWater()
        {
            ClampToTerrain(_area, out Vector2Int origin, out Vector2Int size);

            for (int x = origin.x; x < size.x - 1; x++)
                for (int y = origin.y; y < size.y - 1; y++)
                {
                    _water[x, y] += _rainAmount;
                }
        }

        private void Erode()
        {
            ClampToTerrain(_area, out Vector2Int origin, out Vector2Int size);

            Vector2Int coord = new();
            // Flow + erosion
            for (int x = origin.x; x < size.x - 1; x++)
            {
                for (int y = origin.y; y < size.y - 1; y++)
                {
                    coord.x = x;
                    coord.y = y;

                    float currentHeight = _height[x, y];
                    float currentWater = _water[x, y];

                    // Compute total downhill difference
                    float totalDiff = 0f;
                    float[,] diff = new float[3, 3];

                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            float neighborHeight = _height[x + dx, y + dy];
                            float d = currentHeight - neighborHeight;

                            if (d >= _minSlope)
                            {
                                diff[dx + 1, dy + 1] = d;
                                totalDiff += d;
                            }
                        }

                    if (totalDiff <= 0f)
                        continue;

                    // Sediment capacity (proportional to slope + water)
                    float slope = totalDiff / 8f;
                    float capacity = _capacityConstant * slope * currentWater;
                    // Utils.Log($"At {coord}, height {currentHeight} / {_originalHeight[x, y]}, water {currentWater}, sediment {_sediment[x, y]}, slope {slope}, capacity {capacity}");

                    // Erode or deposit
                    if (_sediment[x, y] < capacity)
                    {
                        float amount = _erosionRate * (capacity - _sediment[x, y]);
                        _sediment[x, y] += amount;
                        _height[x, y] -= amount; // apply integer change conservatively

                        // Utils.Log($"Eroded {amount} at {coord}, new height {_height[x, y]}, sediment now {_sediment[x, y]}, capacity {capacity}");
                    }
                    else
                    {
                        float amount = _depositionRate * (_sediment[x, y] - capacity);
                        _sediment[x, y] -= amount;
                        _height[x, y] += amount;

                        // Utils.Log($"Deposited {amount} at {coord}, new height {_height[x, y]}, sediment now {_sediment[x, y]}, capacity {capacity}");
                    }

                    _height[x, y] = Mathf.Clamp(_height[x, y], 0, _terrainService.MaxTerrainHeight); // prevent excessive erosion/deposition

                    // Distribute water + sediment downhill
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (diff[dx + 1, dy + 1] <= 0f) continue;

                            float share = diff[dx + 1, dy + 1] / totalDiff;

                            float waterFlow = currentWater * share;
                            float sedimentFlow = _sediment[x, y] * share;

                            _water[x, y] -= waterFlow;
                            _sediment[x, y] -= sedimentFlow;

                            _water[x + dx, y + dy] += waterFlow;
                            _sediment[x + dx, y + dy] += sedimentFlow;
                        }
                }
            }

            // Evaporation
            for (int x = origin.x; x < size.x - 1; x++)
            {
                for (int y = origin.y; y < size.y - 1; y++)
                {
                    _water[x, y] *= 1f - _evaporation;
                }
            }
        }
    }
}