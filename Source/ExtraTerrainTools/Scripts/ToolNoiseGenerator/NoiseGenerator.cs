using System;
using System.Collections;
using System.Reflection;
using Timberborn.MapStateSystem;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using Unity.Mathematics;
using UnityEngine;

namespace TerrainTools.NoiseGenerator
{
	public class NoiseGenerator : ILoadableSingleton
	{
		public class Worker : MonoBehaviour
		{
			public static void PostJob(NoiseGenerator generator, NoiseParameters parameters, UpdateMode mode)
			{
				Recruit().AssignJob(generator, parameters, mode);
			}

			private static Worker Recruit()
			{
				return new GameObject("Worker_" + DateTime.Now.GetHashCode()).AddComponent<Worker>();
			}

			private void AssignJob(NoiseGenerator generator, NoiseParameters parameters, UpdateMode mode)
			{
				StartCoroutine(
					generator.GenerateTerrain(parameters, mode, delegate {
						Destroy(gameObject); 
					})
				);
			}
		}

		private static readonly int CellsPerFrame = 256;

		public static NoiseParameters DefaultParameters { get; } = new ( 
			// Seed
			null, 
			// Octaves
			4, 
			// Amplitude
			0.5f,
			// Freqency
			3, 
			// Period X
			4, 
			// Period Y
			1, 
			// Floor
			1,
			// Mid
			8,
			// Ceiling
			16,
			// Base curve
			Easer.Function.Quad,
			// Crest curve
			Easer.Function.Quad
		);
		public static NoiseParameterLimits Limits { get; } = new (
			// Octaves
			1,		8,
			// Amplitude
			1/16f,	1.2f,
			// Freqency
			0.1f,	8,
			// Period X
			0.1f,	8,
			// Period Y
			0.1f,	8,
			// Floor
			1,		16,
			// Mid
			1,		16,
			// Ceiling
			1,		16
		);

		private Randomizer _rng;
		public enum UpdateMode {
			DoNothing,
			ClearExisting,
			UpdateExisting,
		}
        private readonly ITerrainService _terrainService;
		private readonly ResetService _resetService;
		private readonly EventBus _eventBus;
        public NoiseGenerator(
            ITerrainService terrainService,
			ResetService resetService,
			EventBus eventBus
        ) {
            _terrainService = terrainService;
			_resetService = resetService;
			_eventBus = eventBus;
        }

        public void Load()
		{
		}

		public void Generate( NoiseParameters parameters, UpdateMode mode = UpdateMode.UpdateExisting )
		{
			if (mode == UpdateMode.ClearExisting)
			{
				_resetService.ClearEntities();			
			}

			Worker.PostJob(this, parameters, mode);
		}

		private IEnumerator GenerateTerrain(NoiseParameters parameters, UpdateMode mode = UpdateMode.UpdateExisting, Action finalAction = null)
		{
			// I'm on it!
			_eventBus.Post(new GeneratorStartedEvent(this));
			_resetService.PauseEditorSim();

			// Handle seeding
			if (parameters.Seed == null)
				_rng ??= new();
			else
				_rng = new(parameters.Seed);

			// Get dimensions
			Vector3 terrainSize		 = _terrainService.Size;
			FieldInfo maxHeightField = typeof(MapSize).GetField("MaxMapEditorTerrainHeight", BindingFlags.Static | BindingFlags.Public);
			
			int maxHeight 	= (int)maxHeightField.GetValue(null);
			terrainSize.z 	= maxHeight;
		
			// Compute aspect ratio based on dominant axis
			Vector2 aspect = new(
				terrainSize.x < terrainSize.y ? terrainSize.x / terrainSize.y : 1,
				terrainSize.x > terrainSize.y ? terrainSize.y / terrainSize.x : 1
			);

			// Hard cutoff for configured editor limit
			int ceiling = parameters.Ceiling < maxHeight 	? parameters.Ceiling 	: maxHeight,
				floor 	= parameters.Floor > 0 				? parameters.Floor 		: 1;
			
			// Randomize
			Vector2 offset = new(
				_rng.GetFloat(), 
				_rng.GetFloat()
			);
			float rotation = _rng.GetFloat();

			Utils.Log("parameters.Amplitude = {0}", parameters.Amplitude);

			// Loop
			Easer easer 		= new(parameters.Base, parameters.Crest);
			Vector2	coord 		= new();

			float 	n 			= terrainSize.x * terrainSize.y,
					maxAmp		= Limits.Amplitude.Max,
					maxAmp2		= 2 * maxAmp,
				 	result, 
					rescaled,
					eased;

			int 	midOffset 	= parameters.Mid - maxHeight / 2,
					cellsToDo	= CellsPerFrame;

			Utils.Log("Slope params: {0} - {1}",parameters.Base, parameters.Crest);
			for (int y = 0; y < terrainSize.y; y++)
			{
				for (int x = 0; x < terrainSize.x; x++)
				{	
					// Height generation
					coord.x = x / terrainSize.x;
					coord.y = y / terrainSize.y;

					coord.x *= aspect.x;
					coord.y *= aspect.y;
					result = FBM(coord + offset, parameters.Period, rotation, parameters.Octaves, parameters.Amplitude, parameters.Frequency);

					Utils.Log("result = {0}", result);
					rescaled = Mathf.Clamp01((result + 1) / 2f);
					Utils.Log("rescaled = {0}", rescaled);
					eased = easer.Value(rescaled);
					Utils.Log("eased = {0}", eased);

					int z = Mathf.RoundToInt(
						Mathf.Clamp(terrainSize.z * eased + midOffset, floor, ceiling)
					);

					Utils.Log("z = {0}", z);

					_terrainService.SetHeight(new Vector2Int(x, y), z);
				
					// I'm working on it!
					_eventBus.Post(new GeneratorProgressEvent(this, (x + terrainSize.x * y) / n));

					// Chunk handling
					if (cellsToDo > 0)
						cellsToDo--;
					else
					{
						cellsToDo = CellsPerFrame;
						yield return null;
					}
				}
			}

			if( mode == UpdateMode.UpdateExisting)
			{
				_resetService.UpdateEntities();
			}

			// I'm done!
			_resetService.UnpauseEditorSim();
			_eventBus.Post( new GeneratorFinishedEvent(this) );

			// I have to clean up!?
			finalAction?.Invoke();
		}

		public static float FBM(
			Vector2 coord, Vector2 period, float rotation = 0f, 
			int octaves = 6, float amplitude = 0.5f, float frequency = 3 
		) {

			int		oct = octaves;
			float	amp = amplitude,
					freq = frequency,
					value = 0;

			for( int i = 0; i < oct; i++) {
				value += amp * Noise( freq * coord, period, rotation );
				amp  *= 0.5f;
				freq *= 2;
			}

			return value;
		}  

		// Simple wrapper to convert input from Unity.Mathematics.float2 to Vector2
		public static float Noise( Vector2 coord, Vector2 period, float rotation ) {
			return noise.psrnoise( 
				new float2(coord.x,coord.y), 
				new float2(period.x,period.y),
				rotation
			);
		}
    }
}