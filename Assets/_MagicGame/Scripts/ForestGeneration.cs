using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ForestGeneration : MonoBehaviour
{
	[FoldoutGroup("Overworld Generation")]
	[SerializeField] private NoiseMapSO _forestGroundNM;
	
	[FoldoutGroup("Overworld Generation")]
	[SerializeField] private NoiseMapSO _forestStoneNM;
	
	[FoldoutGroup("Overworld Generation")]
	[SerializeField] private TileSO _grassTile;
	
	[FoldoutGroup("Overworld Generation")]
	[SerializeField] private TileSO _sandTile;
	
	[FoldoutGroup("Overworld Generation")]
	[SerializeField] private TileSO _waterTile;
	
	[FoldoutGroup("Overworld Generation")]
	[SerializeField] private TileSO _stoneWallTile;
	
	[FoldoutGroup("Overworld Generation")]
	[SerializeField] private TileSO _stoneFloorTile;
	
	[FoldoutGroup("Overworld Generation")]
	[SerializeField] private WorldObject _treeObject;
	
	
	private string _seed;
	
	public void GenerateForest()
	{
		Debug.Log("Generating Forest Data...");
		ChunkManager.IS_GENERATING_BIOME = true;
		
		// Generate World Data
		GenerateNoiseMapsBasedOnSeed();
		GenerateOverworldChunkData();
		GenerateTrees();
		
		SaveSystem.Instance.AddBiomeToMemorySessionTracker(BiomeType.Forest);
		ChunkManager.IS_GENERATING_BIOME = false;
		Debug.Log("Generating Forest Complete!");
	}
	
	private void GenerateNoiseMapsBasedOnSeed()
	{
		_seed = WorldManager.Instance.Seed;
		
		// Generate noise textures using game manager seed
		_forestGroundNM.GenerateNoiseTexture(_seed);
		_forestStoneNM.GenerateNoiseTexture(_seed);
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
						float groundTilePointValue = GetNoiseMapPointValueAtCoords(_forestGroundNM, tilePosX, tilePosY);
						float wallTilePointValue = GetNoiseMapPointValueAtCoords(_forestStoneNM, tilePosX, tilePosY);
						
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
		// Generate Tree placements
		float minTreeDistance = 3f;
		float maxTreeDistance = 10f;
		
		List<Vector2> treePoints = PoissonDiskSampling.GeneratePoints(_forestStoneNM, minTreeDistance, maxTreeDistance, _seed);
		
		foreach (Vector2 point in treePoints)
		{
			int pointX = Mathf.RoundToInt(point.x);
			int pointY = Mathf.RoundToInt(point.y);
			
			float groundTilePointValue = _forestGroundNM.NoiseTexture.GetPixel(pointX, pointY).grayscale;
			float wallTilePointValue = _forestStoneNM.NoiseTexture.GetPixel(pointX, pointY).grayscale;
			
			if(groundTilePointValue > 0.125f && (wallTilePointValue < 0.6f && wallTilePointValue > 0.35f))
			{
				// Add world asset data to chunk
				ChunkManager.Instance.AddObjectDataToChunk(new Vector2Int(pointX, pointY), _treeObject, BiomeType.Forest);
			}
		}
	}
	
	private TileSO GetOverworldWallTileFromPointValue(float pointValue)
	{
		if(pointValue > 0.4f ) return _stoneWallTile;
		return null;
	}	
	
	private TileSO GetOverworldGroundTileFromPointValue(float pointValue)
	{
		if(pointValue < 0.1f) return _waterTile;
		if(pointValue < 0.125f) return _sandTile;
		return _grassTile;
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
