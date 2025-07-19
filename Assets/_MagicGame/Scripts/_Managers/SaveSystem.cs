using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
	public static SaveSystem Instance { get; private set; }
	
	private BiomeFileData _biomeFileDataForSaving = new();
	private BiomeFileData _biomeFileDataForLoading = new();
	public HashSet<BiomeType> BiomesInMemory { get; private set; } = new();
	private string _path;
	
	public bool IsSaving { get; private set; }
	public bool IsDeserializing { get; private set; }
	
	private void Awake()
	{
		Instance = this;
	}
	
	public void AddBiomeToMemorySessionTracker(BiomeType biome)
	{
		if(!BiomesInMemory.Contains(biome))
		{
			Debug.Log($"Adding biome to biomes in memeory {biome}");
			BiomesInMemory.Add(biome);
		}
	}
	
	public bool BiomeLoadedInMemory(BiomeType biome)
	{
		return BiomesInMemory.Contains(biome);
	}
	
	public bool BiomeSaveFileExists(BiomeType biome)
	{
		return File.Exists(Application.dataPath + $"/_MagicGame/Configuration/JsonData/{biome}_data.json");
	}
	
	#region Serialization
	
	[Button("Save current player environment data")]
	public async void SavePlayerBiome()
	{
		await SerializeDataAndWriteToFile(Player.Instance.CurrentBiome.Value);
	}
	
	public async Task SaveBiome(BiomeType biome)
	{
		IsSaving = true;
		await SerializeDataAndWriteToFile(biome);
		IsSaving = false;
	}
	
	private async Task SerializeDataAndWriteToFile(BiomeType biomeToSave)
	{
		Debug.Log($"<color=orange>=====================SAVING========================</color>");
		Debug.Log($"Path: " + Application.dataPath + $"/_MagicGame/Configuration/JsonData/{biomeToSave}_data.json");
	
		_path = Application.dataPath + $"/_MagicGame/Configuration/JsonData/{biomeToSave}_data.json";
		
		// Serialize Assets and Chunks
		SerializeObjectDataOfCurrentEnvironment(biomeToSave);
		SerializeChunkDataOfCurrentEnvironment(biomeToSave);
		SerializeChestDataOfCurrentEnvironment(biomeToSave);
		
		// Write the data to file
		await WriteCurrentEnvironmentDataToFile();
	}
	
	private void SerializeObjectDataOfCurrentEnvironment(BiomeType biomeToSave)
	{
		// Clear assets
		_biomeFileDataForSaving.WorldObjectsList.Clear();
		
		// Loop through all assets and push them to _sceneData before serializing it
		foreach (var chunkPosChunkDataKVP in ChunkManager.Instance.GetChunksFromBiome(biomeToSave))
		{
			if(chunkPosChunkDataKVP.Value.GetWorldObjects().Count > 0)
			{
				foreach (WorldObjectGameData worldObjectGameData in chunkPosChunkDataKVP.Value.GetWorldObjects())
				{
					// If so, serialize it
					if(worldObjectGameData.WO != null)
					{
						// Create new filedata for this asset
						WorldObjectFileData worldAssetData;
						
						if(worldObjectGameData is DoorObjectGameData doorObjectGameData)
						{
							worldAssetData = new DoorObjectFileData(GameManager.Instance.GetIDFromWorldObject(worldObjectGameData.WO), worldObjectGameData.Position, worldObjectGameData.Orientation, doorObjectGameData.IsOpen);
						}
						else
						{
							worldAssetData = new WorldObjectFileData(GameManager.Instance.GetIDFromWorldObject(worldObjectGameData.WO), worldObjectGameData.Position, worldObjectGameData.Orientation);
						}
						
						// Push it to WorldAssets in sceneData
						_biomeFileDataForSaving.WorldObjectsList.Add(worldAssetData);
					}
				}
			}
		}
		
		Debug.Log($"<color=orange>Asset Data of </color>{biomeToSave}<color=orange> Serialized</color>");
	}
	
	private void SerializeChunkDataOfCurrentEnvironment(BiomeType biomeToSave)
	{
		// Clear world chunks for new data
		_biomeFileDataForSaving.ChunksList.Clear();
		
		// Initialize chunk data to write
		List<ChunkFileData> sceneDataChunks = new();
		
		// Convert chunks into ChunkData for serialization
		foreach (var kvp in ChunkManager.Instance.GetChunksFromBiome(biomeToSave))
		{
			int listSize = kvp.Value.Size * kvp.Value.Size;
		
			ChunkFileData chunkData = new()
			{
				ChunkPosition = kvp.Key,
				Size = kvp.Value.Size,
				GroundTiles = new(listSize),
				FloorTiles = new(listSize),
				WallTiles = new(listSize),
				OreTiles = new(listSize),
				FoliageTiles = new(listSize),
				LiquidTiles = new(listSize)
			};

			var tileGroups = new List<(List<TileGameData> source, List<TileFileData> target)>
			{
				(kvp.Value.GetTileList(TileType.Terrain), chunkData.GroundTiles),
				(kvp.Value.GetTileList(TileType.Floor), chunkData.FloorTiles),
				(kvp.Value.GetTileList(TileType.Wall), chunkData.WallTiles),
				(kvp.Value.GetTileList(TileType.Ore), chunkData.OreTiles),
				(kvp.Value.GetTileList(TileType.Liquid), chunkData.LiquidTiles),
				(kvp.Value.GetTileList(TileType.Foliage), chunkData.FoliageTiles),
			};

			foreach (var (sourceList, targetList) in tileGroups)
			{
				foreach (TileGameData tile in sourceList)
				{
					TileSO tileObjectSO = GameManager.Instance.GetTileSOFromTileBase(tile.TileSO);
					TileFileData tileData = new()
					{
						Pos = tile.TilePosition,
						TileId = GameManager.Instance.GetIDFromTileObjectSO(tileObjectSO),
						TileType = tileObjectSO.TileType
					};

					targetList.Add(tileData);
				}
			}

			// Add chunkData to sceneData
			sceneDataChunks.Add(chunkData);
		}
		
		// Push chunk scene data to current SceneSaveHandler
		_biomeFileDataForSaving.ChunksList = sceneDataChunks;
		Debug.Log($"<color=orange>Chunk Data of </color>{biomeToSave}<color=orange> Serialized</color>");
	}

	private void SerializeChestDataOfCurrentEnvironment(BiomeType biomeToSave)
	{
		_biomeFileDataForSaving.ChestList.Clear();
		
		foreach (var chestData in ChestManager.Instance.GetChestDataFromBiome(biomeToSave))
		{
			List<ItemFileData> chestItemsToSerialize = new();
		
			for (int i = 0; i < chestData.Value.Count; i++)
			{
				if(chestData.Value[i] != null)
				{
					List<int> magicArray = new();
					
					if(chestData.Value[i] is WandInventoryItem wandInventoryItem)
					{
						for (int j = 0; j < wandInventoryItem.MagicArray.Length; j++)
						{
							magicArray.Add(wandInventoryItem.MagicArray[j] != null ? GameManager.Instance.GetItemIdFromItemSO(wandInventoryItem.MagicArray[j]) : -1);
						}
					}
				
					chestItemsToSerialize.Add(new ItemFileData
					{
						SlotIndex = i,
						ItemId = GameManager.Instance.GetItemIdFromItemSO(chestData.Value[i].Item),
						Quantity = chestData.Value[i].Quantity,
						MagicArray = magicArray
					});
				}
			}
		
			_biomeFileDataForSaving.ChestList.Add(new ChestFileData
			{
				ChestPosition = chestData.Key,
				ChestItems = chestItemsToSerialize
			});
		}
	}
	
	private async Task WriteCurrentEnvironmentDataToFile()
	{
		string json = JsonUtility.ToJson(_biomeFileDataForSaving);
		
		Debug.Log($"<color=orange>Writing Biome Data of: </color>{Player.Instance.CurrentBiome.Value}<color=orange> to file...</color>");
		
		await File.WriteAllTextAsync(_path, json);
		
		Debug.Log($"<color=orange>Biome: </color>{Player.Instance.CurrentBiome.Value}<color=orange> writing data to file complete!</color>");
		Debug.Log($"<color=orange>=====================SAVING========================</color>");
	}

	#endregion
	
	#region Deserialization
	
public List<(int WorldObjectId, Vector2Int Position)> RetrieveBiomeTransitionWorldObjectData(BiomeType biomeToLoad)
{
    _path = Application.dataPath + $"/_MagicGame/Configuration/JsonData/{biomeToLoad}_data.json";
    List<(int WorldObjectId, Vector2Int Position)> transitionDataList = new();

    if (File.Exists(_path))
    {
        string json = File.ReadAllText(_path);
        BiomeFileData biomeFileData = JsonUtility.FromJson<BiomeFileData>(json);
        List<WorldObjectFileData> worldObjectFileData = new(biomeFileData.WorldObjectsList);

        // Collect data for each BiomeTransitionObject
        foreach (WorldObjectFileData data in worldObjectFileData)
        {
            if (GameManager.Instance.GetWorldObjectFromID(data.WorldObjectId) is BiomeTransitionObject)
            {
                Debug.Log($"Found BiomeTransitionObject: {data.WorldObjectId} at {data.Pos}");
                transitionDataList.Add((data.WorldObjectId, data.Pos));
            }
        }
    }

    Debug.Log($"Count FROM SAVE SYSTEM: {transitionDataList.Count}");
    return transitionDataList;
}
	
	public async Task DeserializeAndDispatchData(BiomeType biomeToLoad)
	{
		if (BiomesInMemory.Contains(biomeToLoad))
		{
			Debug.Log($"<color=red>{biomeToLoad} is already loaded. Skipping...</color>");
			return; // Skip loading if already loaded
		}
	
		IsDeserializing = true;
	
		_path = Application.dataPath + $"/_MagicGame/Configuration/JsonData/{biomeToLoad}_data.json";
		
		if (File.Exists(_path))
		{
			Debug.Log($"<color=orange>=====================LOADING========================</color>");
			Debug.Log($"Path: " + Application.dataPath + $"/_MagicGame/Configuration/JsonData/{biomeToLoad}_data.json");
			Debug.Log($"<color=orange>Deserializing </color>{biomeToLoad}<color=orange> Data From File...</color>");
			
			// Read JSON data from file asynchronously
			string json = await File.ReadAllTextAsync(_path);
			
			// Deserialize JSON data into SceneData object
			_biomeFileDataForLoading = JsonUtility.FromJson<BiomeFileData>(json);
			
			// Dispatch the data
			DeserializeChunkData(biomeToLoad);
			DeserializeObjectData(biomeToLoad);
			DeserializeChestData(biomeToLoad);
			
			// Mark this biome as loaded
			AddBiomeToMemorySessionTracker(biomeToLoad);
			
			Debug.Log($"<color=orange>Chunk and Asset Data of: </color>{biomeToLoad}<color=orange> Deserialized! </color>");
			Debug.Log($"<color=orange>=====================LOADING========================</color>");
		}
		
		IsDeserializing = false;
		
		// NTFS: Write condition for non existant path
	}

	private void DeserializeChunkData(BiomeType biomeToLoad)
	{
		// Unpack chunk data
		List<ChunkFileData> chunkFileData = _biomeFileDataForLoading.ChunksList;
		
		// Construct a new Dictionary<Vector2Int, Chunk> of deserialized chunk data and send it to ChunkManager to use
		Dictionary<Vector2Int, ChunkGameData> deserializedChunks = new Dictionary<Vector2Int, ChunkGameData>();
		
		// Convert ChunkData into Chunks, push it to deserializedChunks and send that to ChunkManager
		foreach (ChunkFileData data in chunkFileData)
		{
			ChunkGameData chunk = new(data.Size, data.ChunkPosition);
			chunk.DeserializeTileLists(
				ConvertTileFileDataToGameData(data.GroundTiles),
				ConvertTileFileDataToGameData(data.LiquidTiles),
				ConvertTileFileDataToGameData(data.FloorTiles),
				ConvertTileFileDataToGameData(data.WallTiles),
				ConvertTileFileDataToGameData(data.OreTiles),
				ConvertTileFileDataToGameData(data.FoliageTiles)
			);
			deserializedChunks.Add(data.ChunkPosition, chunk);
		}
		
		// Update player chunks so assets can deserialize properly
		ChunkManager.Instance.LoadChunksForBiome(biomeToLoad, deserializedChunks);
		
		Debug.Log($"<color=orange>Chunk Data of: </color>{biomeToLoad}<color=orange> Deserialized And Updated </color>");
	}
	
	// Convert tile file data to tile game data
	private List<TileGameData> ConvertTileFileDataToGameData(List<TileFileData> tileFileData)
	{
		int count = tileFileData.Count;
		List<TileGameData> tileGameData = new(count); // ✅ pre-size list

		for (int i = 0; i < count; i++)
		{
			TileSO tileSO = GameManager.Instance.GetTileSOFromID(tileFileData[i].TileId);
			tileGameData.Add(new TileGameData(tileSO, tileFileData[i].Pos));
		}

		return tileGameData;
	}
	
	private void DeserializeObjectData(BiomeType biomeToDeserialize)
	{
		// Unpack asset data need to make a new list to avoid error that said I was modifying this list as it was being processed
		List<WorldObjectFileData> worldObjectFileData = new(_biomeFileDataForLoading.WorldObjectsList);
		
		// Instantiate each asset
		foreach (WorldObjectFileData data in worldObjectFileData)
		{
			// Fetch each prefab from database
			WorldObject worldObjectToInst = GameManager.Instance.GetWorldObjectFromID(data.WorldObjectId);
			ChunkManager.Instance.DeserializeObjectDataToChunk(data, biomeToDeserialize, worldObjectToInst, data.Orientation);
		}
		
		Debug.Log($"<color=orange>Asset Data of: </color>{biomeToDeserialize}<color=orange> Deserialized</color>");
	}
	
	private void DeserializeChestData(BiomeType biomeToLoad)
	{
		List<ChestFileData> chestDataList = new(_biomeFileDataForLoading.ChestList);
		ChestManager.Instance.GetChestDataFromBiome(biomeToLoad).Clear();
		
		foreach (ChestFileData chestData in chestDataList)
		{
			// Convert file data to game data
			List<InventoryItem> chestItemsGameData = new();
			
			for (int i = 0; i < ChestManager.CHEST_CAPACITY; i++)
			{
				chestItemsGameData.Add(new InventoryItem() { Item = null, Quantity = 0 });
			}
			
			foreach (ItemFileData item in chestData.ChestItems)
			{
				ItemSO itemToAdd = GameManager.Instance.GetItemSOFromItemId(item.ItemId);
				
				if(itemToAdd is WandItemSO wandItemSO)
				{
					WandInventoryItem wandInventoryItem = new WandInventoryItem(itemToAdd, item.Quantity, wandItemSO.Capacity);

					for (int i = 0; i < item.MagicArray.Count; i++)
					{
						if(item.MagicArray[i] > -1)
						{
							wandInventoryItem.SetMagic(GameManager.Instance.GetItemSOFromItemId(item.MagicArray[i]) as SpellItemSO, i);
						}
					}

					chestItemsGameData[item.SlotIndex] = wandInventoryItem;
				}
				else
				{
					chestItemsGameData[item.SlotIndex] = new InventoryItem(itemToAdd, item.Quantity);
				}
			}
			
			ChestManager.Instance.AddChestEntry(chestData.ChestPosition, chestItemsGameData, biomeToLoad);
		}
		
	}
	
	#endregion
}
