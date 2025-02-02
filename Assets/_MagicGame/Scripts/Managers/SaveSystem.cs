using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
	public event EventHandler OnSerializationFinished;
	public event EventHandler OnDeserializationFinished;

	public event EventHandler OnNoFileFoundToDeserialize;

	public static SaveSystem Instance { get; private set; }
	
	private EnvironmentFileData _environmentFileData = new();
	private string _path;
	
	public bool IsSerializing { get; private set; }
	public bool IsDeserializing { get; private set; }
	
	private void Awake()
	{
		Instance = this;
	}
	
	public bool EnvironmentDataExists(BiomeType environment)
	{
		string path = Application.dataPath + $"/_MagicGame/Configuration/JsonData/{environment}_data.json";
		
		return File.Exists(path);
	}
	
	#region Serialization
	
	[Button("Save current player environment data")]
	public async void SavePlayer()
	{
		await SerializeDataAndWriteToFile(Player.LocalClientInstance.CurrentBiome.Value);
	}
	
	public async Task SerializeDataAndWriteToFile(BiomeType environmentToSerialize)
	{
		IsSerializing = true;
	
		_path = Application.dataPath + $"/_MagicGame/Configuration/JsonData/{environmentToSerialize}_data.json";
		Debug.Log($"<color=orange>=============================================</color>");
		Debug.Log($"Path: " + Application.dataPath + $"/_MagicGame/Configuration/JsonData/{environmentToSerialize}_data.json");
		// Serialize Assets and Chunks
		SerializeObjectDataOfCurrentEnvironment(environmentToSerialize);
		SerializeChunkDataOfCurrentEnvironment(environmentToSerialize);
		SerializeChestDataOfCurrentEnvironment(environmentToSerialize);
		
		// Write the data to file
		await WriteCurrentEnvironmentDataToFile();
	}
	
	private void SerializeObjectDataOfCurrentEnvironment(BiomeType environmentToSerialize)
	{
		// Clear assets
		_environmentFileData.WorldObjectsList.Clear();
		
		// Loop through all assets and push them to _sceneData before serializing it
		foreach (var chunkPosChunkDataKVP in ChunkManager.Instance.GetChunksFromBiome(environmentToSerialize))
		{
			List<WorldObjectGameData> worldObjectGameDataList = chunkPosChunkDataKVP.Value.WorldObjectGameDataList;
		
			if(worldObjectGameDataList.Count > 0)
			{
				foreach (WorldObjectGameData worldObjectGameData in worldObjectGameDataList)
				{
					// If so, serialize it
					if(worldObjectGameData.Asset != null)
					{
						// Create new filedata for this asset
						WorldObjectFileData worldAssetData;
						
						if(worldObjectGameData is DoorObjectGameData doorObjectGameData)
						{
							Debug.Log($"Serializing door. door open: {doorObjectGameData.IsOpen}");
							worldAssetData = new DoorObjectFileData(GameManager.Instance.GetByteIDFromWorldObject(worldObjectGameData.Asset), worldObjectGameData.Position, doorObjectGameData.IsOpen);
						}
						else
						{
							worldAssetData = new WorldObjectFileData(GameManager.Instance.GetByteIDFromWorldObject(worldObjectGameData.Asset), worldObjectGameData.Position);
						}
						
						// Push it to WorldAssets in sceneData
						_environmentFileData.WorldObjectsList.Add(worldAssetData);
					}
				}
			}
		}
		
		Debug.Log($"<color=orange>Asset Data of </color>{environmentToSerialize}<color=orange> Serialized</color>");
	}
	
	private void SerializeChunkDataOfCurrentEnvironment(BiomeType environmentToSerialize)
	{
		// Clear world chunks for new data
		_environmentFileData.ChunksList.Clear();
		
		// Initialize chunk data to write
		List<ChunkFileData> sceneDataChunks = new();
		
		// Convert chunks into ChunkData for serialization
		foreach (var kvp in ChunkManager.Instance.GetChunksFromBiome(environmentToSerialize))
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
		_environmentFileData.ChunksList = sceneDataChunks;
		Debug.Log($"<color=orange>Chunk Data of </color>{environmentToSerialize}<color=orange> Serialized</color>");
	}

	private void SerializeChestDataOfCurrentEnvironment(BiomeType environmentToSerialize)
	{
		_environmentFileData.ChestList.Clear();
		
		foreach (var chestData in ChestManager.Instance.GetChestDataFromEnvironment(environmentToSerialize))
		{
			List<ItemFileData> chestItemsToSerialize = new();
		
			foreach (ChestItemData chestItemData in chestData.Value)
			{
				chestItemsToSerialize.Add(new ItemFileData
				{
					SlotIndex = chestItemData.SlotIndex,
					ItemId = chestItemData.ItemId,
					Quantity = chestItemData.Quantity
				});
			}
		
			_environmentFileData.ChestList.Add(new ChestFileData
			{
				ChestPosition = chestData.Key,
				ChestItems = chestItemsToSerialize
			});
		}
	}
	
	private async Task WriteCurrentEnvironmentDataToFile()
	{
		// Serialize scene data to JSON
		string json = JsonUtility.ToJson(_environmentFileData);
		
		// Log the saving
		Debug.Log($"<color=orange>Writing Environment Data of: </color>{Player.LocalClientInstance.CurrentBiome.Value}<color=orange> to file...</color>");
		
		// Write JSON data to file asynchronously
		await File.WriteAllTextAsync(_path, json);
		
		// Log the completion
		Debug.Log($"<color=orange>Environment: </color>{Player.LocalClientInstance.CurrentBiome.Value}<color=orange> writing data to file complete!</color>");
		
		OnSerializationFinished?.Invoke(this, EventArgs.Empty);
		
		IsSerializing = false;
	}

	#endregion
	
	#region Deserialization
	
	[Button("Deserialize data of current environment and dispatch updated data to game")]
	public async Task DeserializeAndDispatchData(BiomeType environmentToDeserialize)
	{
		IsDeserializing = true;
	
		_path = Application.dataPath + $"/_MagicGame/Configuration/JsonData/{environmentToDeserialize}_data.json";
		
		if (File.Exists(_path))
		{
			Debug.Log($"<color=orange>=============================================</color>");
			Debug.Log($"Path: " + Application.dataPath + $"/_MagicGame/Configuration/JsonData/{environmentToDeserialize}_data.json");
			Debug.Log($"<color=orange>Deserializing </color>{environmentToDeserialize}<color=orange> Data From File...</color>");
			
			// Read JSON data from file asynchronously
			string json = await File.ReadAllTextAsync(_path);
			
			// Deserialize JSON data into SceneData object
			_environmentFileData = JsonUtility.FromJson<EnvironmentFileData>(json);
			
			// Dispatch the data
			DeserializeChunkData(environmentToDeserialize);
			DeserializeObjectData(environmentToDeserialize);
			DeserializeChestData(environmentToDeserialize);
			
			Debug.Log($"<color=orange>Chunk and Asset Data of: </color>{environmentToDeserialize}<color=orange> Deserialized! </color>");
			
			OnDeserializationFinished?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			OnNoFileFoundToDeserialize?.Invoke(this, EventArgs.Empty);
		}
		
		IsDeserializing = false;
		
		// NTFS: Write condition for non existant path
	}

	private void DeserializeChunkData(BiomeType environmentToDeserialize)
	{
		// Unpack chunk data
		List<ChunkFileData> chunkFileData = _environmentFileData.ChunksList;
		
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
			
			// Set up Nodes to be walkable for pathfinding
			foreach (TileGameData wallTileGameData in chunk.WallTileGameDataList)
			{
				var tileWorldPosition = wallTileGameData.TilePosition;
			}
			
			deserializedChunks.Add(data.ChunkPosition, chunk);
		}
		
		// Update player chunks so assets can deserialize properly
		ChunkManager.Instance.SetChunksForEnvironment(environmentToDeserialize, deserializedChunks);
		// ChunkManager.Instance.SinglePlayerUpdatePlayerChunks();
		
		Debug.Log($"<color=orange>Chunk Data of: </color>{environmentToDeserialize}<color=orange> Deserialized And Updated </color>");
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
	
	private void DeserializeObjectData(BiomeType biomeToDeserialize)
	{
		// Unpack asset data need to make a new list to avoid error that said I was modifying this list as it was being processed
		List<WorldObjectFileData> worldObjectFileData = new(_environmentFileData.WorldObjectsList);
		
		// Clear all scene assets
		// AssetManager.Instance.ClearAllCurrentEnvironmentAssets();
		
		// Instantiate each asset
		foreach (WorldObjectFileData data in worldObjectFileData)
		{
			if(data is DoorObjectFileData doorData)
			{
				Debug.Log($"FOund door file data. Is open?: {doorData.IsOpen}");
			}
		
			// Fetch each prefab from database
			WorldObject worldObjectToInst = GameManager.Instance.GetWorldObjectFromID(data.WorldObjectId);
			ChunkManager.Instance.AddObjectDataToChunk(data, biomeToDeserialize, worldObjectToInst);
		}
		
		Debug.Log($"<color=orange>Asset Data of: </color>{biomeToDeserialize}<color=orange> Deserialized</color>");
	}
	
	private void DeserializeChestData(BiomeType environmentToDeserialize)
	{
		List<ChestFileData> chestDataList = new(_environmentFileData.ChestList);
		ChestManager.Instance.GetChestDataFromEnvironment(environmentToDeserialize).Clear();
		
		
		foreach (ChestFileData chestData in chestDataList)
		{
			// Convert file data to game data
			List<ChestItemData> chestItemsGameData = new();
			foreach (ItemFileData item in chestData.ChestItems)
			{
				chestItemsGameData.Add(new ChestItemData()
				{
					SlotIndex = item.SlotIndex,
					ItemId = item.ItemId,
					Quantity = item.Quantity
				});
			}
			
			ChestManager.Instance.AddChestEntry(chestData.ChestPosition, chestItemsGameData, environmentToDeserialize);
		}
		
	}
	
	#endregion
}
