using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CaveGeneration : MonoBehaviour
{
	[FoldoutGroup("Cave Generation")]
	[SerializeField] private NoiseMapSO _spaghettiCaveNM;
	
	[FoldoutGroup("Cave Generation")]
	[SerializeField] private NoiseMapSO _cheeseCaveNM;

	[FoldoutGroup("Cave Generation")]
	[SerializeField] private NoiseMapSO _oreGenNM;
	
	[FoldoutGroup("Cave Generation")]
	[SerializeField] private TileSO _stoneWallTile;
	
	[FoldoutGroup("Cave Generation")]
	[SerializeField] private TileSO _stoneFloorTile;

	[FoldoutGroup("Cave Generation")]
	[SerializeField] private TileSO _cobaltOreTile;

	private string _seed;

	public void GenerateCave()
	{
		Debug.Log("Generating Cave...");
		ChunkManager.IS_GENERATING_BIOME = true;

		// Generate noise textures using game manager seed
		_seed = WorldManager.Instance.Seed;
		_spaghettiCaveNM.GenerateNoiseTexture(_seed);
		_cheeseCaveNM.GenerateNoiseTexture(_seed);
		_oreGenNM.GenerateNoiseTexture(_seed);

		GenerateCaveChunkData();

		SaveSystem.Instance.AddBiomeToMemorySessionTracker(BiomeType.Cave);
		ChunkManager.IS_GENERATING_BIOME = false;
		
		Debug.Log("Cave Generation Complete!");
	}

	private void GenerateCaveChunkData()
	{
		ChunkManager.Instance.GetChunksFromBiome(BiomeType.Cave).Clear();
		
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
						
						float cheeseCaveValue = GetNoiseMapPointValueAtCoords(_cheeseCaveNM, tilePosX, tilePosY);
						float spaghettiCaveValue = GetNoiseMapPointValueAtCoords(_spaghettiCaveNM, tilePosX, tilePosY);
						float oreGenValue = GetNoiseMapPointValueAtCoords(_oreGenNM, tilePosX, tilePosY);

						TryToAddTileToChunk(_stoneFloorTile, tileWorldPosition, chunkGameData.GroundTileGameDataList);

						if (spaghettiCaveValue < 0.45f || spaghettiCaveValue > 0.6f && cheeseCaveValue < 0.375f)
						{
							// Adding a cave wall
							if(oreGenValue > 0.05f)
							{
								TryToAddTileToChunk(_cobaltOreTile, tileWorldPosition, chunkGameData.WallTileGameDataList);
							}
							else
							{
								TryToAddTileToChunk(_stoneWallTile, tileWorldPosition, chunkGameData.WallTileGameDataList);
							}
						}
					}
				}
				
				// Populate the overworld chunk data
				ChunkManager.Instance.GetChunksFromBiome(BiomeType.Cave)[chunkCoord] = chunkGameData;
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
}
