using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CaveGeneration : MonoBehaviour
{
    [SerializeField] private WorldManager.EnvironmentID _environment;
	
    [FoldoutGroup("Cave Generation")]
    [SerializeField] private NoiseMapSO _caveSpaghettiCaveNoiseMap;
	
    [FoldoutGroup("Cave Generation")]
    [SerializeField] private NoiseMapSO _caveCheeseCaveNoiseMap;
	
    // [FoldoutGroup("Cave Generation")]
    // [SerializeField] private TileDataBaseObject _tileDatabase;
	
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
        if(WorldManager.Instance.GetActiveEnvironmentID() == _environment)
        {
            GenerateCave();
        }
    }
	
    public void GenerateCave()
    {
        Debug.Log("Generating Cave...");
        GenerateNoiseMapsBasedOnSeed();
        GenerateCaveChunkData();
        GenerateStaircases();
        ChunkManager.Instance.SinglePlayerUpdatePlayerChunks();
        Debug.Log("Cave Generation Complete!");
    }
	
    private void GenerateNoiseMapsBasedOnSeed()
    {
        _seed = WorldManager.Instance.Seed;
		
        // Generate noise textures using game manager seed
        _caveSpaghettiCaveNoiseMap.GenerateNoiseTexture(_seed);
        _caveCheeseCaveNoiseMap.GenerateNoiseTexture(_seed);
    }
	
    private void GenerateCaveChunkData()
    {
        ChunkManager.Instance.GetCaveChunks().Clear();
		
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
						
                        float cheeseCaveValue = GetNoiseMapPointValueAtCoords(_caveCheeseCaveNoiseMap, tilePosX, tilePosY);
                        float spaghettiCaveValue = GetNoiseMapPointValueAtCoords(_caveSpaghettiCaveNoiseMap, tilePosX, tilePosY);
						
                        if((spaghettiCaveValue >= 0.45f && spaghettiCaveValue <= 0.6f) || cheeseCaveValue >= 0.375f)
                        {
                            TryToAddTileToChunk(_stoneFloorTile, tileWorldPosition, chunkGameData.GroundTileGameDataList);
                        }
                        else
                        {
                            TryToAddTileToChunk(_stoneWallTile, tileWorldPosition, chunkGameData.WallTileGameDataList);
                        }
                    }
                }
				
                // Populate the overworld chunk data
                ChunkManager.Instance.GetCaveChunks()[chunkCoord] = chunkGameData;
            }
        }
    }
	
    private void GenerateStaircases()
    {
		
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
