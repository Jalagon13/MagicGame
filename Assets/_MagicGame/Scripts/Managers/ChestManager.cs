using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ChestItemData
{
	public int SlotIndex;
	public int ItemId;
	public int Quantity;
}

public class ChestManager : NetworkBehaviour
{
	public static ChestManager Instance { get; private set; }
	public event EventHandler OnChestClose;
	public event EventHandler<ChestEventArgs> OnChestOpen;
	public event EventHandler<ChestEventArgs> OnChestUpdated;
	public class ChestEventArgs : EventArgs
	{
		public List<ChestItemData> ChestItemData;
	}

	public bool IsChestOpen { get; private set; } = false;
	public Vector2Int OpenChestPosition { get; private set; }
	public List<string> OpenedChestIds { get; private set; }
	
	[SerializeField] private float _chestCloseDistance = 3f; 
	
	private Dictionary<Vector2Int, List<ChestItemData>> _forestChests = new();
	private Dictionary<Vector2Int, List<ChestItemData>> _caveChests = new();
	private ChestNetworkManager _chestNetworkManager;
	private List<ChestItemData> _localChestItemData; // Used for clients to be sent to server when done editing it

	private void Awake()
	{
		Instance = this;
		_chestNetworkManager = GetComponent<ChestNetworkManager>();
		OpenedChestIds = new();
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
	
	private void CloseChestOnInventoryClose(object sender, GameInput.OnToggleInventoryEventArgs e)
	{
		if(!e.InventoryOpen)
		{
			CloseChest();
		}
	}
	
	public Dictionary<Vector2Int, List<ChestItemData>> GetChestDataFromEnvironment(BiomeType environment)
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

	public void OpenChest(Vector2Int chestPosition, BiomeType playerEnvironment)
	{
		if(Player.LocalClientInstance.IsHost)
		{
			OpenChestHost(chestPosition, playerEnvironment);
		}
		else
		{
			_chestNetworkManager.OpenChestClient(chestPosition, playerEnvironment);
		}
	}
	
	private void OpenChestHost(Vector2Int chestPosition, BiomeType playerEnvironment)
	{
		var environmentChestData = GetChestDataFromEnvironment(playerEnvironment);
	
		if (environmentChestData.ContainsKey(chestPosition))
		{
			string chestId = $"{chestPosition}{playerEnvironment}";
			if(!OpenedChestIds.Contains(chestId))
			{
				OpenedChestIds.Add(chestId);
		
				OpenChest(chestPosition, environmentChestData[chestPosition]);
			}
		}
		else
		{
			Debug.LogError($"Chest not found for position: {chestPosition}. This message should never play; chest data should always be found when opening.");
		}
	}
	
	public void OpenChest(Vector2Int chestPosition, List<ChestItemData> chestData)
	{
		if(IsChestOpen == false)
		{
			InventoryManager.Instance.OnInventorySlotShiftLeftClicked += EnableChestShortcuts;
		}
		
		OpenChestPosition = chestPosition;
		IsChestOpen = true;
		
		if(!IsServer)
		{
			// If not server, turn chestData into a local data structure and have the left and right click functionalities, edit that local data structure
			_localChestItemData = chestData;
		}
			
		OnChestOpen?.Invoke(this, new ChestEventArgs
		{
			ChestItemData = chestData
		});
	}

	public void CloseChest()
	{
		if (IsChestOpen)
		{
			_chestNetworkManager.RemoveChestId(OpenChestPosition, Player.LocalClientInstance.CurrentBiome.Value);
		
			InventoryManager.Instance.OnInventorySlotShiftLeftClicked -= EnableChestShortcuts;
			IsChestOpen = false;

			if(!IsServer)
			{
				_chestNetworkManager.UpdateChestContents(OpenChestPosition, Player.LocalClientInstance.CurrentBiome.Value, _localChestItemData);
			}
			
			OnChestClose?.Invoke(this, EventArgs.Empty);
		}
	}
	
	public void AddChestEntry(Vector2Int chestPosition, List<ChestItemData> chestItems, BiomeType environment)
	{
		GetChestDataFromEnvironment(environment).Add(chestPosition, chestItems);
	}

	public void TryToCreateEmptyChestData(Vector2Int chestPosition, BiomeType environment)
	{
		if (GetChestDataFromEnvironment(environment).ContainsKey(chestPosition))
		{
			return;
		}

		// Create an entry for this position with an empty chest
		GetChestDataFromEnvironment(environment).Add(chestPosition, new List<ChestItemData>());
	}

	public void RemoveChestData(Vector2Int chestPosition, BiomeType environment)
	{
		if (GetChestDataFromEnvironment(environment).ContainsKey(chestPosition))
		{
			GetChestDataFromEnvironment(environment).Remove(chestPosition);
			Debug.Log($"Chest entry removed for position: {chestPosition}");
		}
	}
	
	public void UpdateChestSlots()
	{
		OnChestUpdated?.Invoke(this, new ChestEventArgs
		{
			ChestItemData = IsServer ? GetChestDataFromEnvironment(Player.LocalClientInstance.CurrentBiome.Value)[OpenChestPosition] : _localChestItemData
		});
	}
	
	private void EnableChestShortcuts(object sender, InventoryManager.ShortCutInventoryItemEventArgs e)
	{
		// NTFS: Shift Click chest functionality to be added here
		if(e.InventoryItem.HasItem)
		{
			// If it is stackable
			if(e.InventoryItem.Item.Stackable)
			{
				int chestCapacity = 18;
				for (int i = 0; i < chestCapacity; i++)
				{
					var chestItemData = GetChestItemEntry(i);
					bool isOccupied = chestItemData != null;
					
					if(isOccupied)
					{
						// Is it the same item?
						if(e.InventoryItem.Item.Name == GameManager.Instance.GetItemSOFromItemId(chestItemData.ItemId).Name)
						{
							// Add it to chest slot and break
							GetChestItemEntry(i).Quantity += e.InventoryItem.Quantity;
							InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = new(); // NTFS: No max stack wrap around functionality built in yet
							break;
						}
					}
					else
					{
						int quantity = InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex].Quantity;
						int id = GameManager.Instance.GetItemIdFromItemSO(e.InventoryItem.Item);
						AddChestItemEntry(i, id, quantity);
						InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = new();
						break;
					}
				}
			}
			else // Not stackable
			{
				// Loop through all the chests to find an empty spot and place it there
				int chestCapacity = 18;
				for (int i = 0; i < chestCapacity; i++)
				{
					var chestItemData = GetChestItemEntry(i);
					bool isOccupied = chestItemData != null;
				
					if(!isOccupied)
					{
						// Move the item to this chest slot and stop
						AddChestItemEntry(i, GameManager.Instance.GetItemIdFromItemSO(e.InventoryItem.Item), 1);
						Debug.Log($"asdfasdf");
						InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = new();
						break;
					}
				}
			}
			
			InventoryManager.Instance.GetInventoryModel().UpdateInventory();
			UpdateChestSlots();
		}
	}
	
	public void ChestSlotRightClicked(int clickedChestSlotIndex)
	{
		// Define variables at the top, just like in ChestSlotRightClicked
		ChestItemData openChestSlotItemData = GetChestItemEntry(clickedChestSlotIndex);
		InventoryItem openChestSlotInventoryItem = openChestSlotItemData == null ? new() : new(GameManager.Instance.GetItemSOFromItemId(openChestSlotItemData.ItemId), openChestSlotItemData.Quantity);
		InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;

		bool chestSlotHasItem = openChestSlotItemData != null;

		if (chestSlotHasItem)
		{
			if (mouseItem.HasItem) // Normal functionality
			{
				if (openChestSlotInventoryItem.Item.Name == mouseItem.Item.Name)
				{
					GetChestItemEntry(clickedChestSlotIndex).Quantity += 1;
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity -= 1;

					if (InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity <= 0)
					{
						InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
					}
				}
				else
				{
					// Swap the two items
					InventoryItem tempItem = openChestSlotInventoryItem;
					GetChestItemEntry(clickedChestSlotIndex).ItemId = GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item);
					GetChestItemEntry(clickedChestSlotIndex).Quantity = mouseItem.Quantity;
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = tempItem;
				}
			}
			else
			{
				int openChestSlotItemQuantity = openChestSlotInventoryItem.Quantity;
				int newChestSlotItemQuantity = openChestSlotItemQuantity / 2;
				int newMouseItemQuantity = openChestSlotItemQuantity - newChestSlotItemQuantity;

				GetChestItemEntry(clickedChestSlotIndex).Quantity = newChestSlotItemQuantity;
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = openChestSlotInventoryItem.Item;
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = newMouseItemQuantity;

				if (GetChestItemEntry(clickedChestSlotIndex).Quantity == 0)
				{
					RemoveChestItemEntry(clickedChestSlotIndex);
				}

				// TooltipManager.Instance.Hide();
			}
		}
		else
		{
			if (mouseItem.HasItem)
			{
				AddChestItemEntry(clickedChestSlotIndex, GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item), 1);
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity -= 1;

				if (InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity <= 0)
				{
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
					// TooltipManager.Instance.Show(mouseItem is SpellBookInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
				}
			}
		}

		// Play click feedbacks and update Inventory
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
		UpdateChestSlots();
	}

	public void ChestSlotLeftClicked(int clickedChestSlotIndex)
	{
		// Define variables at the top, just like in ChestSlotLeftClicked
		ChestItemData openChestSlotItemData = GetChestItemEntry(clickedChestSlotIndex);
		InventoryItem openChestSlotInventoryItem = openChestSlotItemData == null ? new() : new(GameManager.Instance.GetItemSOFromItemId(openChestSlotItemData.ItemId), openChestSlotItemData.Quantity);
		InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;

		bool chestSlotHasItem = openChestSlotItemData != null;

		if (chestSlotHasItem)
		{
			if (mouseItem.HasItem)
			{
				if (openChestSlotInventoryItem.Item.Name == mouseItem.Item.Name && mouseItem.Item.Stackable)
				{
					// If the items are the same and stackable, add the mouse item's quantity to the chest slot
					GetChestItemEntry(clickedChestSlotIndex).Quantity += mouseItem.Quantity;
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
					
					// TooltipManager.Instance.Show(mouseItem is SpellBookInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
				}
				else
				{
					// Swap the two items
					InventoryItem tempItem = openChestSlotInventoryItem;
					GetChestItemEntry(clickedChestSlotIndex).ItemId = GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item);
					GetChestItemEntry(clickedChestSlotIndex).Quantity = mouseItem.Quantity;
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = tempItem;
				}
			}
			else
			{
				// If the mouse has no item, pick up the chest slot's item
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem = openChestSlotInventoryItem;
				RemoveChestItemEntry(clickedChestSlotIndex);
				
				// TooltipManager.Instance.Hide();
			}
		}
		else
		{
			if (mouseItem.HasItem)
			{
				// If the chest slot is empty and the mouse has an item, place the item in the chest slot
				Debug.Log($"Chest slot index clicked {clickedChestSlotIndex}");
				AddChestItemEntry(clickedChestSlotIndex, GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item), mouseItem.Quantity);

				InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
				
				// TooltipManager.Instance.Show(mouseItem is SpellBookInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
			}
		}

		// Update the inventory and play click feedbacks
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
		UpdateChestSlots();
	}
	
	private ChestItemData GetChestItemEntry(int chestSlotIndexToGet)
	{
		// Determine the correct chest data container based on whether this is the server or client
		var chestItemDataContainer = IsServer 
			? GetChestDataFromEnvironment(Player.LocalClientInstance.CurrentBiome.Value)[OpenChestPosition] 
			: _localChestItemData;

		// Find and return the chest item data for the specified slot index
		foreach (ChestItemData chestItemData in chestItemDataContainer)
		{
			if (chestItemData.SlotIndex == chestSlotIndexToGet)
			{
				return chestItemData;
			}
		}

		// Debug.LogWarning($"This chest does not have an entry to get at this chest slot index {chestSlotIndexToGet}");
		return null;
	}

	public void RemoveChestItemEntry(int chestSlotIndex)
	{
		// Determine the correct chest data container based on whether this is the server or client
		var chestItemDataContainer = IsServer 
			? GetChestDataFromEnvironment(Player.LocalClientInstance.CurrentBiome.Value)[OpenChestPosition] 
			: _localChestItemData;

		// Remove the chest item data for the specified slot index
		foreach (ChestItemData chestItemData in chestItemDataContainer)
		{
			if (chestItemData.SlotIndex == chestSlotIndex)
			{
				chestItemDataContainer.Remove(chestItemData);
				return;
			}
		}
	}

	private void AddChestItemEntry(int chestSlotIndex, int itemId, int quantity)
	{
		// Determine the correct chest data container based on whether this is the server or client
		var chestItemDataContainer = IsServer 
			? GetChestDataFromEnvironment(Player.LocalClientInstance.CurrentBiome.Value)[OpenChestPosition] 
			: _localChestItemData;

		// Add a new chest item entry to the container
		chestItemDataContainer.Add(new()
		{
			SlotIndex = chestSlotIndex,
			ItemId = itemId,
			Quantity = quantity
		});
	}
	
	public override void OnDestroy()
	{
		GameInput.Instance.OnInventoryToggle -= CloseChestOnInventoryClose;
	}
}