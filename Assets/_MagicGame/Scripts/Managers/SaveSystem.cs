using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SaveSystem : MonoBehaviour
{
	public event EventHandler OnSerializationFinished;
	public event EventHandler OnDeserializationFinished;

	public event EventHandler OnNoFileFoundToDeserialize;

	public static SaveSystem Instance { get; private set; }
	
	private EnvironmentFileData _environmentFileData = new();
	private string _path;
	
	private void Awake()
	{
		Instance = this;
	}
	
	[Button("Serialize data of current environment and write to file")]
	public async Task SerializeDataAndWriteToFile()
	{
		_path = Application.dataPath + $"/_MagicGame/Configuration/JsonData/{WorldManager.Instance.GetActiveEnvironmentID()}_data.json";
	
		Debug.Log($"<color=orange>=============================================</color>");
		// Serialize Assets and Chunks
		SerializeAssetDataOfCurrentEnvironment();
		SerializeChunkDataOfCurrentEnvironment();
		
		// Write the data to file
		await WriteCurrentEnvironmentDataToFile();
	}

	private async Task WriteCurrentEnvironmentDataToFile()
	{
		// Serialize scene data to JSON
		string json = JsonUtility.ToJson(_environmentFileData);
		
		// Log the saving
		Debug.Log($"<color=orange>Writing Environment Data of: </color>{WorldManager.Instance.GetActiveEnvironmentID()}<color=orange> to file...</color>");
		
		// Write JSON data to file asynchronously
		await File.WriteAllTextAsync(_path, json);
		
		// Log the completion
		Debug.Log($"<color=orange>Environment: </color>{WorldManager.Instance.GetActiveEnvironmentID()}<color=orange> writing data to file complete!</color>");
		
		OnSerializationFinished?.Invoke(this, EventArgs.Empty);
	}

	private void SerializeChunkDataOfCurrentEnvironment()
	{
		// Clear world chunks for new data
		_environmentFileData.WorldChunks.Clear();
		
		// Initialize chunk data to write
		List<ChunkFileData> sceneDataChunks = new();
		
		// Convert chunks into ChunkData for serialization
		foreach (var kvp in ChunkManager.Instance.GetChunksFromActiveEnvironment())
		{
			ChunkFileData chunkData = new()
			{
				ChunkPosition = kvp.Key,
				Size = kvp.Value.Size,
				GroundTiles = new(),
				WallTiles = new(),
			};

			// Loop through each tile in ground tiles add create a serializable TileData for it and add it to chunkData
			foreach (TileGameData tile in kvp.Value.GroundTileGameDataList)
			{
				TileSO tileObjectSO = GameManager.Instance.GetTileSOFromTileBase(tile.TileSO);
				TileFileData tileData = new()
				{
					Pos = tile.TilePosition,
					TileId = GameManager.Instance.GetByteIDFromTileObjectSO(tileObjectSO),
					TileType = tileObjectSO.TileType
				};
				
				chunkData.GroundTiles.Add(tileData);
			}
			
			// Loop through each tile in ground tiles add create a serializable TileData for it and add it to chunkData
			foreach (TileGameData tile in kvp.Value.WallTileGameDataList)
			{
				TileSO tileObjectSO = GameManager.Instance.GetTileSOFromTileBase(tile.TileSO);
				TileFileData tileData = new()
				{
					Pos = tile.TilePosition,
					TileId = GameManager.Instance.GetByteIDFromTileObjectSO(tileObjectSO),
					TileType = tileObjectSO.TileType
				};
				
				chunkData.WallTiles.Add(tileData);
			}
			
			// Add chunkData to sceneData
			sceneDataChunks.Add(chunkData);
		}
		
		// Push chunk scene data to current SceneSaveHandler
		_environmentFileData.WorldChunks = sceneDataChunks;
		Debug.Log($"<color=orange>Chunk Data of </color>{WorldManager.Instance.GetActiveEnvironmentID()}<color=orange> Serialized</color>");
	}
	
	private void SerializeAssetDataOfCurrentEnvironment()
	{
		// Clear assets
		_environmentFileData.WorldAssets.Clear();
		
		// Loop through all assets and push them to _sceneData before serializing it
		foreach (var chunkPosChunkDataKVP in ChunkManager.Instance.GetChunksFromActiveEnvironment())
		{
			var worldAssetGameDataList = chunkPosChunkDataKVP.Value.WorldAssetGameDataList;
		
			if(worldAssetGameDataList.Count > 0)
			{
				foreach (var worldAssetGameData in worldAssetGameDataList)
				{
					// If so, serialize it
					if(worldAssetGameData.Asset != null)
					{
						// Create new filedata for this asset
						WorldAssetFileData worldAssetData = new()
						{
							WorldAssetId = GameManager.Instance.GetByteIDFromWorldObject(worldAssetGameData.Asset),
							Pos = worldAssetGameData.Position
						};
						
						// Push it to WorldAssets in sceneData
						_environmentFileData.WorldAssets.Add(worldAssetData);
					}
				}
			}
		}
		
		Debug.Log($"<color=orange>Asset Data of </color>{WorldManager.Instance.GetActiveEnvironmentID()}<color=orange> Serialized</color>");
	}
	
	[Button("Deserialize data of current environment and dispatch updated data to game")]
	public async Task DeserializeAndDispatchData()
	{
		_path = Application.dataPath + $"/_MagicGame/Configuration/JsonData/{WorldManager.Instance.GetActiveEnvironmentID()}_data.json";
		
		if (File.Exists(_path))
		{
			Debug.Log($"<color=orange>=============================================</color>");
			Debug.Log($"<color=orange>Deserializing </color>{WorldManager.Instance.GetActiveEnvironmentID()}<color=orange> Data From File...</color>");
			
			// Read JSON data from file asynchronously
			string json = await File.ReadAllTextAsync(_path);
			
			// Deserialize JSON data into SceneData object
			_environmentFileData = JsonUtility.FromJson<EnvironmentFileData>(json);
			
			// Dispatch the data
			DeserializeChunkData();
			DeserializeAssetData();
			
			Debug.Log($"<color=orange>Chunk and Asset Data of: </color>{WorldManager.Instance.GetActiveEnvironmentID()}<color=orange> Deserialized! </color>");
			
			OnDeserializationFinished?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			OnNoFileFoundToDeserialize?.Invoke(this, EventArgs.Empty);
		}
		
		// NTFS: Write condition for non existant path
	}
	
	private void DeserializeChunkData()
	{
		// Unpack chunk data
		List<ChunkFileData> chunkFileData = _environmentFileData.WorldChunks;
		
		// Construct a new Dictionary<Vector2Int, Chunk> of deserialized chunk data and send it to ChunkManager to use
		Dictionary<Vector2Int, ChunkGameData> deserializedChunks = new Dictionary<Vector2Int, ChunkGameData>();
		
		// Convert ChunkData into Chunks, push it to deserializedChunks and send that to ChunkManager
		foreach (ChunkFileData data in chunkFileData)
		{
			ChunkGameData chunk = new(data.Size, data.ChunkPosition)
			{
				GroundTileGameDataList = ConvertTileFileDataToGameData(data.GroundTiles),
				WallTileGameDataList = ConvertTileFileDataToGameData(data.WallTiles),
			};
			
			deserializedChunks.Add(data.ChunkPosition, chunk);
		}
		
		// Update player chunks so assets can deserialize properly
		ChunkManager.Instance.SetChunksFromActiveEnvironment(deserializedChunks);
		// ChunkManager.Instance.SinglePlayerUpdatePlayerChunks();
		
		Debug.Log($"<color=orange>Chunk Data of: </color>{WorldManager.Instance.GetActiveEnvironmentID()}<color=orange> Deserialized And Updated </color>");
	}
	
	// Convert tile file data to tile game data
	private List<TileGameData> ConvertTileFileDataToGameData(List<TileFileData> tileFileData)
	{
		List<TileGameData> tileGameData = new();
		
		foreach (TileFileData data in tileFileData)
		{
			TileSO tileSO = GameManager.Instance.GetTileSOFromID(data.TileId);
			TileGameData tile = new(tileSO, data.Pos);
			
			tileGameData.Add(tile);
		}
		
		return tileGameData;
	}
	
	private void DeserializeAssetData()
	{
		// Unpack asset data need to make a new list to avoid error that said I was modifying this list as it was being processed
		List<WorldAssetFileData> worldAssetFileData = new(_environmentFileData.WorldAssets);
		
		// Clear all scene assets
		// AssetManager.Instance.ClearAllCurrentEnvironmentAssets();
		
		// Instantiate each asset
		Debug.Log($"World Object File Data Count: {worldAssetFileData.Count}");
		foreach (WorldAssetFileData data in worldAssetFileData)
		{
			// Fetch each prefab from database
			WorldObject worldObjectToInst = GameManager.Instance.GetWorldObjectFromID(data.WorldAssetId);
			ChunkManager.Instance.AddWorldAssetDataToChunk(data.Pos, worldObjectToInst);
		}
		
		Debug.Log($"<color=orange>Asset Data of: </color>{WorldManager.Instance.GetActiveEnvironmentID()}<color=orange> Deserialized</color>");
	}
}
