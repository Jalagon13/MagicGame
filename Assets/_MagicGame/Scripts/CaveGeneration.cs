using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CaveGeneration : MonoBehaviour
{
	[field: SerializeField] public WorldObject StairsToForest { get; private set; }
	[field: SerializeField] public ForestGeneration ForestGeneration { get; private set; }

	[Header("Noise Maps")]
	[field: SerializeField] public NoiseMapSO SpaghettiCaveNM;
	[field: SerializeField] public NoiseMapSO CheeseCaveNM;
	[field: SerializeField] public NoiseMapSO OreGenNM;

	[Header("Tiles")]
	[field: SerializeField] public TileSO StoneWallTile;
	[field: SerializeField] public TileSO StoneFloorTile;
	[field: SerializeField] public TileSO CobaltOreTile;

	private string _seed;
	private BiomeType _biomeType = BiomeType.Cave;
	private HashSet<Vector2Int> _stairsToCavePositions = new HashSet<Vector2Int>();

	private void Start()
	{
		Initialization();
	}

	public void GenerateCave()
	{
		Debug.Log("Generating Cave...");
		ChunkManager.IS_GENERATING_BIOME = true;

		Initialization();
		GenerateCaveChunkData();
		PlaceStairsToForest();

		SaveSystem.Instance.AddBiomeToMemorySessionTracker(_biomeType);
		ChunkManager.IS_GENERATING_BIOME = false;
		
		Debug.Log("Cave Generation Complete!");
	}
	
	private void Initialization()
	{
		// Generate noise textures using game manager seed
		_seed = WorldManager.Instance.Seed;
		SpaghettiCaveNM.GenerateNoiseTexture(_seed);
		CheeseCaveNM.GenerateNoiseTexture(_seed);
		OreGenNM.GenerateNoiseTexture(_seed);
	}

    private void GenerateCaveChunkData()
	{
		_stairsToCavePositions = ForestGeneration.GetStairsToCavePositions(_seed);
		ChunkManager.Instance.GetChunksFromBiome(_biomeType).Clear();
		
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
						
						float cheeseCaveValue = GetNoiseMapPointValueAtCoords(CheeseCaveNM, tilePosX, tilePosY);
						float spaghettiCaveValue = GetNoiseMapPointValueAtCoords(SpaghettiCaveNM, tilePosX, tilePosY);
						float oreGenValue = GetNoiseMapPointValueAtCoords(OreGenNM, tilePosX, tilePosY);

						TryToAddTileToChunk(StoneFloorTile, tileWorldPosition, chunkGameData.GroundTileGameDataList);
						
						if(_stairsToCavePositions.Contains(tileWorldPosition))
						{
							continue;
						}

						if (spaghettiCaveValue < 0.45f || spaghettiCaveValue > 0.6f && cheeseCaveValue < 0.375f)
						{
							// Adding a cave wall
							if(oreGenValue > 0.05f)
							{
								TryToAddTileToChunk(CobaltOreTile, tileWorldPosition, chunkGameData.OreTileGameDataList);
							}

							TryToAddTileToChunk(StoneWallTile, tileWorldPosition, chunkGameData.WallTileGameDataList);
						}
					}
				}
				
				// Populate the overworld chunk data
				ChunkManager.Instance.GetChunksFromBiome(_biomeType)[chunkCoord] = chunkGameData;
			}
		}
	}

	private void PlaceStairsToForest()
	{
		foreach(Vector2Int position in _stairsToCavePositions)
		{
			DeleteNeighborWallsAroundPoint(position);
			ChunkManager.Instance.AddObjectDataToChunkServerRpc(position, GameManager.Instance.GetIDFromWorldObject(StairsToForest), _biomeType, CardinalDirection.North);
		}
	}

	private void DeleteNeighborWallsAroundPoint(Vector2Int centerPosition)
	{
		// Nested for loop to check all surrounding tiles within a 3x3 grid centered on the given position
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				Vector2Int neighborPosition = new(centerPosition.x + x, centerPosition.y + y);
				ChunkManager.Instance.RemoveTileServerRpc(TileType.Wall, neighborPosition, _biomeType);
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
