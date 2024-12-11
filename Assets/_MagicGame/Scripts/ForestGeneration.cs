using System;
using System.Collections.Generic;
// using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ForestGeneration : MonoBehaviour
{
    [SerializeField] private WorldManager.EnvironmentID _environment;

    [FoldoutGroup("Overworld Generation")]
    [SerializeField] private NoiseMapSO _overworldGoundNoiseMap;
	
    [FoldoutGroup("Overworld Generation")]
    [SerializeField] private NoiseMapSO _overworldWallNoiseMap;
	
    [FoldoutGroup("Overworld Generation")]
    [SerializeField] private TileDataBaseSO _tileDatabase;
	
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
    [SerializeField] private WorldObject _resourceObject;
	
	
    private string _seed;
	
    private void Start()
    {
        SaveSystem.Instance.OnNoFileFoundToDeserialize += SaveSystem_OnNoFileFoundToDeserialize;
    }

    private void SaveSystem_OnNoFileFoundToDeserialize(object sender, EventArgs e)
    {
        if(WorldManager.Instance.GetActiveEnvironmentID() == _environment)
        {
            GenerateOverworld();
        }
    }

    public void GenerateOverworld()
    {
        Debug.Log("Generating Overworld Data...");
        ChunkManager.IS_GENERATING_ENVIRONMENT = true;
        GenerateNoiseMapsBasedOnSeed();
        GenerateOverworldChunkData();
        GenerateTrees();
        ChunkManager.IS_GENERATING_ENVIRONMENT = false;
        Debug.Log("Overworld Data Generation Complete!");
    }
	
    private void GenerateNoiseMapsBasedOnSeed()
    {
        _seed = WorldManager.Instance.Seed;
		
        // Generate noise textures using game manager seed
        _overworldGoundNoiseMap.GenerateNoiseTexture(_seed);
        _overworldWallNoiseMap.GenerateNoiseTexture(_seed);
    }
	
    private void GenerateOverworldChunkData()
    {
        ChunkManager.Instance.GetOverworldChunks().Clear();
		
        // Loop through all chunks
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
						
                        // For the position of the tile, find out which tile to place here using its grayscale value
                        float groundTilePointValue = GetNoiseMapPointValueAtCoords(_overworldGoundNoiseMap, tilePosX, tilePosY);
                        float wallTilePointValue = GetNoiseMapPointValueAtCoords(_overworldWallNoiseMap, tilePosX, tilePosY);
						
                        // Get the Tilebase for the given point value
                        TileSO groundTileSO = GetOverworldGroundTileFromPointValue(groundTilePointValue);
                        TileSO wallTileSO = GetOverworldWallTileFromPointValue(wallTilePointValue);
						
                        // Initialize the tile game data and store it in the chunk
                        TryToAddTileToChunk(groundTileSO, tileWorldPosition, chunkGameData.GroundTileGameDataList);
						
                        // NTFS: Obviously temporary generation code for walls
                        if(wallTilePointValue >= 0.6f)
                        {
                            TryToAddTileToChunk(wallTileSO, tileWorldPosition, chunkGameData.WallTileGameDataList);
							
                            var centerNodePosition = new Vector2(tileWorldPosition.x + 0.5f, tileWorldPosition.y + 0.5f);
                            // var node = NodeGraphUtility.GetNodeAtPosition(centerNodePosition);
                            // node.Walkable = false;
                        }
                    }
                }
				
                // Populate the overworld chunk data
                ChunkManager.Instance.GetOverworldChunks().Add(chunkCoord, chunkGameData);
            }
        }
    }
	
    private void GenerateTrees()
    {
        // Generate Tree placements
        Vector2Int surfaceBounds = new Vector2Int(ChunkManager.ENVIRONMENT_SIDE_LENGTH, ChunkManager.ENVIRONMENT_SIDE_LENGTH);
		
        float minTreeDistance = 3f;
        float maxTreeDistance = 10f;
		
        List<Vector2> treePoints = PoissonDiskSampling.GeneratePoints(_overworldWallNoiseMap, minTreeDistance, maxTreeDistance, surfaceBounds, _seed);
		
        foreach (Vector2 point in treePoints)
        {
            int pointX = Mathf.RoundToInt(point.x);
            int pointY = Mathf.RoundToInt(point.y);
			
            float groundTilePointValue = _overworldGoundNoiseMap.NoiseTexture.GetPixel(pointX, pointY).grayscale;
            float wallTilePointValue = _overworldWallNoiseMap.NoiseTexture.GetPixel(pointX, pointY).grayscale;
			
            if(groundTilePointValue > 0.125f && (wallTilePointValue < 0.6f && wallTilePointValue > 0.35f))
            {
                // Add world asset data to chunk
                ChunkManager.Instance.AddWorldAssetDataToChunk(new Vector2Int(pointX, pointY), _resourceObject);
            }
        }
    }
	
    // NTFS: Maybe use this for something else later. Right now not in use
    // private void GenerateTransitions()
    // {
    // 	// Spawn staircases randomly using Possion Disk Sampling
    // 	Vector2Int surfaceBounds = new Vector2Int(256, 256);
		
    // 	float minStaircaseDistance = 40f;
    // 	float maxStaircaseDistance = 40f;
		
    // 	List<Vector2> points = PoissonDiskSampling.GeneratePoints(_overworldGoundNoiseMap, minStaircaseDistance, maxStaircaseDistance, surfaceBounds, _seed);
    // 	List<Vector2Int> stairPositions = new();
		
    // 	// Loop through all points and only generate ones that fit in the island elevation
    // 	foreach (Vector2 point in points)
    // 	{
    // 		int pointX = Mathf.RoundToInt(point.x);
    // 		int pointY = Mathf.RoundToInt(point.y);
    // 		float groundTilePointValue = _overworldGoundNoiseMap.NoiseTexture.GetPixel(pointX, pointY).grayscale;
			
    // 		if(groundTilePointValue > 0.2f)
    // 		{
    // 			// Instantiate(_environmentTransitionAsset.gameObject, new(pointX, pointY), Quaternion.identity);
    // 		}
    // 	}
		
    // 	// Take all stair positions and "push" them to GameManager
    // 	// GameManager.Instance.SetStaircasePositions(SceneNames.Island, stairPositions);
    // }
	
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
	
    private void OnDestroy()
    {
        SaveSystem.Instance.OnNoFileFoundToDeserialize -= SaveSystem_OnNoFileFoundToDeserialize;
    }
}
