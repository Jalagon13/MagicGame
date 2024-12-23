using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CaveGeneration : MonoBehaviour
{
	[SerializeField] private EnvironmentID _environment;
	
	[FoldoutGroup("Cave Generation")]
	[SerializeField] private NoiseMapSO _spaghettiCaveNM;
	
	[FoldoutGroup("Cave Generation")]
	[SerializeField] private NoiseMapSO _cheeseCaveNM;
	
	[FoldoutGroup("Cave Generation")]
	[SerializeField] private TileSO _stoneWallTile;
	
	[FoldoutGroup("Cave Generation")]
	[SerializeField] private TileSO _stoneFloorTile;
	
	
	private string _seed;
	
	private void Start()
	{
		SaveSystem.Instance.OnNoFileFoundToDeserialize += SaveSystem_OnNoFileFoundToDeserialize;
	}

	private void SaveSystem_OnNoFileFoundToDeserialize(object sender, EventArgs e)
	{
		if(Player.LocalClientInstance.GetPlayerEnvironment() == _environment)
		{
			GenerateCave();
		}
	}
	
	public async void GenerateCave()
	{
		Debug.Log("Generating Cave...");
		ChunkManager.IS_GENERATING_ENVIRONMENT = true;
		
		// Create gridgraph for pathfinding for environment if haven't done so already
		NodeGraphUtility.TryToCreateGridGraph(_environment);
		
		// Generate World Data
		GenerateNoiseMapsBasedOnSeed();
		GenerateCaveChunkData();
		
		ChunkManager.IS_GENERATING_ENVIRONMENT = false;
		
		Debug.Log("Cave Generation Complete!");
		
		await SaveSystem.Instance.SerializeDataAndWriteToFile(_environment);
	}
	
	private void GenerateNoiseMapsBasedOnSeed()
	{
		_seed = WorldManager.Instance.Seed;
		
		// Generate noise textures using game manager seed
		_spaghettiCaveNM.GenerateNoiseTexture(_seed);
		_cheeseCaveNM.GenerateNoiseTexture(_seed);
	}
	
	private void GenerateCaveChunkData()
	{
		ChunkManager.Instance.GetChunksFromEnvironment(EnvironmentID.Cave).Clear();
		
		int chunkSideAmount = ChunkManager.ENVIRONMENT_SIDE_LENGTH / ChunkManager.CHUNK_SIZE;
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
						
						float cheeseCaveValue = GetNoiseMapPointValueAtCoords(_cheeseCaveNM, tilePosX, tilePosY);
						float spaghettiCaveValue = GetNoiseMapPointValueAtCoords(_spaghettiCaveNM, tilePosX, tilePosY);
						
						if((spaghettiCaveValue >= 0.45f && spaghettiCaveValue <= 0.6f) || cheeseCaveValue >= 0.375f)
						{
							TryToAddTileToChunk(_stoneFloorTile, tileWorldPosition, chunkGameData.GroundTileGameDataList);
						}
						else
						{
							TryToAddTileToChunk(_stoneWallTile, tileWorldPosition, chunkGameData.WallTileGameDataList);
							
							var centerNodePosition = new Vector2(tileWorldPosition.x + 0.5f, tileWorldPosition.y + 0.5f);
							NodeGraphUtility.SetNodeToWalkable(centerNodePosition, _environment, false);
						}
					}
				}
				
				// Populate the overworld chunk data
				ChunkManager.Instance.GetChunksFromEnvironment(EnvironmentID.Cave)[chunkCoord] = chunkGameData;
			}
		}
	}
	
	private void TryToAddTileToChunk(TileSO tileSO, Vector2Int position, List<TileGameData> chunkDataTileList)
	{
		if(tileSO != null)
		{
			TileGameData tileGameData = new(tileSO, position);
			chunkDataTileList.Add(tileGameData);
		}
	}
	
	private float GetNoiseMapPointValueAtCoords(NoiseMapSO noiseMapSO, int x, int y)
	{
		return noiseMapSO.NoiseTexture.GetPixel(x, y).grayscale;
	}
	
	private void OnDestroy()
	{
		SaveSystem.Instance.OnNoFileFoundToDeserialize -= SaveSystem_OnNoFileFoundToDeserialize;
	}
}
