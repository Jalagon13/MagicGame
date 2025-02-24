using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ChestManager : NetworkBehaviour
{
	public static int CHEST_CAPACITY { get; private set; } = 18;

	public static ChestManager Instance { get; private set; }
	public event EventHandler OnChestClose;
	public event EventHandler<ChestEventArgs> OnChestOpen;
	public event EventHandler<ChestEventArgs> OnChestUpdated;
	public class ChestEventArgs : EventArgs
	{
		public List<InventoryItem> ChestItemData;
	}

	public bool IsChestOpen { get; private set; } = false;
	public Vector2Int OpenChestPosition { get; private set; }
	public List<string> OpenedChestIds { get; private set; }
	
	[SerializeField] private float _chestCloseDistance = 3f; 
	
	private Dictionary<Vector2Int, List<InventoryItem>> _forestChests = new();
	private Dictionary<Vector2Int, List<InventoryItem>> _caveChests = new();
	private ChestNetworkManager _chestNetworkManager;
	private List<InventoryItem> _localChestItemData; // Used for clients to be sent to server when done editing it

	private void Awake()
	{
		Instance = this;
		OpenedChestIds = new();
		_chestNetworkManager = GetComponent<ChestNetworkManager>();
	}
	
	private void Start()
	{
		GameInput.Instance.OnInventoryToggle += CloseChestOnInventoryClose;
	}

	private void Update()
	{
		if (Player.LocalClientInstance == null || !IsChestOpen) return;

		var playerPosition = Player.LocalClientInstance.transform.position;
		var chestPosition = new Vector2(OpenChestPosition.x + 0.5f, OpenChestPosition.y + 0.5f);

		float distance = Vector2.Distance(playerPosition, chestPosition);

		if (distance > _chestCloseDistance)
		{
			CloseChest();
		}
	}

	public List<InventoryItem> GetOpenChestInventoryItems()
	{
		return _localChestItemData;
	}
	
	private void CloseChestOnInventoryClose(object sender, GameInput.OnToggleInventoryEventArgs e)
	{
		if(!e.InventoryOpen)
		{
			CloseChest();
		}
	}
	
	public Dictionary<Vector2Int, List<InventoryItem>> GetChestDataFromBiome(BiomeType environment)
	{
		switch(environment)
		{
			case BiomeType.Forest:
				return _forestChests;
			case BiomeType.Cave:
				return _caveChests;
		}

		Debug.LogError($"Environment {environment} should exist but doesn't, add environment chunks to ChestManager");
		return null;
	}

	public void OpenChest(Vector2Int chestPosition, BiomeType biome)
	{
		if (IsChestOpen)
		{
			CloseChest();
		}

		_chestNetworkManager.OpenChestClient(chestPosition, biome);
	}
	
	public void OpenChest(Vector2Int chestPosition, List<InventoryItem> chestData)
	{
		if(IsChestOpen == false)
		{
			InventoryManager.Instance.OnInventorySlotShiftLeftClicked += EnableChestShortcuts;
		}

		InventoryManager.Instance.OnInventorySlotClicked += UpdateSlots;

		OpenChestPosition = chestPosition;
		IsChestOpen = true;

		_localChestItemData = chestData;

		OnChestOpen?.Invoke(this, new ChestEventArgs
		{
			ChestItemData = _localChestItemData
		});
	}

	public void CloseChest()
	{
		if (IsChestOpen)
		{
			_chestNetworkManager.RemoveChestId(OpenChestPosition, Player.LocalClientInstance.CurrentBiome.Value);
		
			InventoryManager.Instance.OnInventorySlotShiftLeftClicked -= EnableChestShortcuts;
			InventoryManager.Instance.OnInventorySlotClicked -= UpdateSlots;
			IsChestOpen = false;
			
			_chestNetworkManager.UpdateChestContents(OpenChestPosition, Player.LocalClientInstance.CurrentBiome.Value, _localChestItemData);

			OnChestClose?.Invoke(this, EventArgs.Empty);
		}
	}
	
	public void AddChestEntry(Vector2Int chestPosition, List<InventoryItem> chestItems, BiomeType biome)
	{
		GetChestDataFromBiome(biome).Add(chestPosition, chestItems);
	}

	public void TryToCreateEmptyChestData(Vector2Int chestPosition, BiomeType biome)
	{
		if (GetChestDataFromBiome(biome).ContainsKey(chestPosition))
		{
			return;
		}

		// Create an entry for this position with an empty chest
		var emptyChest = new List<InventoryItem>();
		
		for (int i = 0; i < CHEST_CAPACITY; i++)
		{
			emptyChest.Add(new InventoryItem() { Item = null, Quantity = 0 });
		}

		GetChestDataFromBiome(biome).Add(chestPosition, emptyChest);
	}

	public void RemoveChestData(Vector2Int chestPosition, BiomeType environment)
	{
		if (GetChestDataFromBiome(environment).ContainsKey(chestPosition))
		{
			GetChestDataFromBiome(environment).Remove(chestPosition);
			Debug.Log($"Chest entry removed for position: {chestPosition}");
		}
	}

	private void UpdateSlots(object sender, EventArgs e)
	{
		OnChestUpdated?.Invoke(this, new ChestEventArgs
		{
			ChestItemData = _localChestItemData
		});
	}
	
	private void EnableChestShortcuts(object sender, InventoryManager.ShortCutInventoryItemEventArgs e)
	{
		// NTFS below is super buggy. Need to figure out how to distinguish chests from inventory slots. because this code will run on both inventory and chest slots
		return;
	
		// NTFS: Shift Click chest functionality to be added here
		// if(e.InventoryItem.HasItem)
		// {
		// 	// If item I want to add is stackable
		// 	if (e.InventoryItem.Item.Stackable)
		// 	{
		// 		// Check if the item already exists in the inventory
		// 		for (int i = 0; i < _localChestItemData.Count; i++)
		// 		{
		// 			if (!_localChestItemData[i].HasItem) continue; // If slot is empty, move on to the next slot to check

		// 			if (_localChestItemData[i].Item.Name == e.InventoryItem.Item.Name)
		// 			{
		// 				_localChestItemData[i].Quantity += e.InventoryItem.Quantity;
		// 				return;
		// 			}
		// 		}

		// 		// If Item cannot be found in inventory, check for first empty slot
		// 		for (int j = 0; j < _localChestItemData.Count; j++)
		// 		{
		// 			// If empty spot found, override this spot
		// 			if (!_localChestItemData[j].HasItem)
		// 			{
		// 				// Override this slot with itemToAdd
		// 				_localChestItemData[j] = e.InventoryItem;

		// 				if (!_localChestItemData[j].Item.Stackable)
		// 				{
		// 					_localChestItemData[j].Quantity = 1;
		// 				}
		// 				return;
		// 			}
		// 		}
		// 	}
		// 	else // If item is not stackable
		// 	{
		// 		// Loop through all slots
		// 		for (int j = 0; j < _localChestItemData.Count; j++)
		// 		{
		// 			// If the slot is empty, override this spot
		// 			if (!_localChestItemData[j].HasItem)
		// 			{
		// 				// Override this spot with itemToAdd
		// 				_localChestItemData[j] = e.InventoryItem;

		// 				if (!_localChestItemData[j].Item.Stackable)
		// 				{
		// 					_localChestItemData[j].Quantity = 1;
		// 				}
		// 				return;
		// 			}
		// 		}
		// 	}

		// 	// Inventory is full functionality (implement this later) 
		// 	// (implement logic for adding unstackable items when inventory is full as well)
		// 	// (Also impelement logic for wand functionality in this regard as well)
		// }
	}
	
	public override void OnDestroy()
	{
		GameInput.Instance.OnInventoryToggle -= CloseChestOnInventoryClose;
	}
}