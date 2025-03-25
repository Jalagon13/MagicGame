using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ForestGeneration : MonoBehaviour
{
	[field: SerializeField] public WorldObject TreeObject { get; private set; }
	[field: SerializeField] public WorldObject StairsToCave { get; private set; }
	[field: SerializeField] public float MinTreeDistance { get; private set; } = 3f;
	[field: SerializeField] public float MaxTreeDistance { get; private set; } = 8.5f;
	[field: SerializeField] public float MinPortalDistance { get; private set; } = 25f;
	[field: SerializeField] public float MaxPortalDistance { get; private set; } = 45f;

	[Header("Noise Maps")]
	[field: SerializeField] public NoiseMapSO ForestGroundNM;
	[field: SerializeField] public NoiseMapSO ForestStoneNM;

	[Header("Tiles")]
	[field: SerializeField] public TileSO GrassTile;
	[field: SerializeField] public TileSO SandTile;
	[field: SerializeField] public TileSO WaterTile;
	[field: SerializeField] public TileSO StoneWallTile;
	[field: SerializeField] public TileSO StoneFloorTile;
	
	private string _seed;

    private void Start()
    {
		Initialization();
	}

    public void GenerateForest()
	{
		Debug.Log("Generating Forest Data...");
		ChunkManager.IS_GENERATING_BIOME = true;

		// Generate noise textures using game manager seed
		Initialization();
		GenerateOverworldChunkData();
		GenerateTrees();
		GenerateStairsToCave();

		SaveSystem.Instance.AddBiomeToMemorySessionTracker(BiomeType.Forest);
		ChunkManager.IS_GENERATING_BIOME = false;
		Debug.Log("Generating Forest Complete!");
	}
	
	private void Initialization()
	{
		_seed = WorldManager.Instance.Seed;
		ForestGroundNM.GenerateNoiseTexture(_seed);
		ForestStoneNM.GenerateNoiseTexture(_seed);
	}
	
	private void GenerateOverworldChunkData()
	{
		ChunkManager.Instance.GetChunksFromBiome(BiomeType.Forest).Clear();
		
		// Loop through all chunks
		int chunkSideAmount = ChunkManager.BIOME_SIDE_LENGTH / ChunkManager.CHUNK_SIZE;
		for (int chunkX = 0; chunkX < chunkSideAmount; chunkX++)
		{
			for (int chunkY = 0; chunkY < chunkSideAmount; chunkY++)
			{
				// Create a new chunk populate it with tiles in the chunk coord
				Vector2Int chunkCoord = new(chunkX, chunkY);
				ChunkGameData chunkGameData = new(ChunkManager.CHUNK_SIZE, chunkCoord);
				
				// Loop through all positions inside this chunk
				for (int x = 0; x < ChunkManager.CHUNK_SIZE; x++)
				{
					for (int y = 0; y < ChunkManager.CHUNK_SIZE; y++)
					{
						// Get the world position of each tile in the chunk
						int tilePosX = chunkCoord.x * ChunkManager.CHUNK_SIZE + x;
						int tilePosY = chunkCoord.y * ChunkManager.CHUNK_SIZE + y;
						Vector2Int tileWorldPosition = new(tilePosX, tilePosY);
						
						// For the position of the tile, find out which tile to place here using its grayscale value
						float groundTilePointValue = GetNoiseMapPointValueAtCoords(ForestGroundNM, tilePosX, tilePosY);
						float wallTilePointValue = GetNoiseMapPointValueAtCoords(ForestStoneNM, tilePosX, tilePosY);
						
						// Get the Tilebase for the given point value
						TileSO groundTileSO = GetOverworldGroundTileFromPointValue(groundTilePointValue);
						TileSO wallTileSO = GetOverworldWallTileFromPointValue(wallTilePointValue);
						
						// Initialize the tile game data and store it in the chunk
						TryToAddTileToChunk(groundTileSO, tileWorldPosition, chunkGameData.GroundTileGameDataList);
						
						// NTFS: Obviously temporary generation code for walls
						if(wallTilePointValue >= 0.6f)
						{
							TryToAddTileToChunk(wallTileSO, tileWorldPosition, chunkGameData.WallTileGameDataList);
						}
					}
				}
				
				// Populate the overworld chunk data
				ChunkManager.Instance.GetChunksFromBiome(BiomeType.Forest).Add(chunkCoord, chunkGameData);
			}
		}
	}
	
	private void GenerateTrees()
	{
		List<Vector2> treePoints = PoissonDiskSampling.GeneratePoints(ForestStoneNM, MinTreeDistance, MaxTreeDistance, _seed);
		
		foreach (Vector2 point in treePoints)
		{
			int pointX = Mathf.RoundToInt(point.x);
			int pointY = Mathf.RoundToInt(point.y);
			
			float groundTilePointValue = ForestGroundNM.NoiseTexture.GetPixel(pointX, pointY).grayscale;
			float wallTilePointValue = ForestStoneNM.NoiseTexture.GetPixel(pointX, pointY).grayscale;
			
			if(groundTilePointValue > 0.125f && wallTilePointValue < 0.6f && wallTilePointValue > 0.35f)
			{
				// Add world asset data to chunk
				ChunkManager.Instance.AddObjectDataToChunk(new Vector2Int(pointX, pointY), TreeObject, BiomeType.Forest);
			}
		}
	}
	
	public HashSet<Vector2Int> GetStairsToCavePositions(string seed)
	{
		HashSet<Vector2Int> positions = new HashSet<Vector2Int>();
	
		List<Vector2> points = PoissonDiskSampling.GeneratePoints(ForestStoneNM, MinPortalDistance, MaxPortalDistance, seed);

		foreach (Vector2 point in points)
		{
			int pointX = Mathf.RoundToInt(point.x);
			int pointY = Mathf.RoundToInt(point.y);

			float groundTilePointValue = ForestGroundNM.NoiseTexture.GetPixel(pointX, pointY).grayscale;
			float wallTilePointValue = ForestStoneNM.NoiseTexture.GetPixel(pointX, pointY).grayscale;

			if (groundTilePointValue > 0.125f && wallTilePointValue < 0.6f && wallTilePointValue > 0.35f)
			{
				var pos = new Vector2Int(pointX, pointY);
				positions.Add(pos);
			}
		}
		
		return positions;
	}
	
	private void GenerateStairsToCave()
	{
		foreach (Vector2Int pos in GetStairsToCavePositions(_seed))
		{
			ChunkManager.Instance.AddObjectDataToChunk(pos, StairsToCave, BiomeType.Forest);
		}
	}
	
	private TileSO GetOverworldWallTileFromPointValue(float pointValue)
	{
		if(pointValue > 0.4f ) return StoneWallTile;
		return null;
	}	
	
	private TileSO GetOverworldGroundTileFromPointValue(float pointValue)
	{
		if(pointValue < 0.1f) return WaterTile;
		if(pointValue < 0.125f) return SandTile;
		return GrassTile;
	}
	
	private float GetNoiseMapPointValueAtCoords(NoiseMapSO noiseMapSO, int x, int y)
	{
		return noiseMapSO.NoiseTexture.GetPixel(x, y).grayscale;
	}
	
	private void TryToAddTileToChunk(TileSO tileObjectSO, Vector2Int position, List<TileGameData> chunkDataTileList)
	{
		if(tileObjectSO != null)
		{
			TileGameData tileGameData = new(tileObjectSO, position);
			chunkDataTileList.Add(tileGameData);
		}
	}
}
