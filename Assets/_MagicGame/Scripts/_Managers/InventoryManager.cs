using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdvancedTooltips.Core;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
	public static InventoryManager Instance { get; private set; }
	public static int HOTBAR_SLOTS_AMOUNT = 9;
	public static bool MOUSE_HAS_ITEM { get; private set; }
	
	public event EventHandler OnInventorySlotClicked;
	public event EventHandler<ShortCutInventoryItemEventArgs> OnInventorySlotShiftLeftClicked;
	public event EventHandler<ShortCutInventoryItemEventArgs> OnInventorySlotShiftRightClicked;
	public class ShortCutInventoryItemEventArgs : EventArgs
	{
		public InventoryItem InventoryItem;
		public int SlotIndex;
	}
	
	public event EventHandler<InventoryItemEventArgs> OnMouseItemUpdated;
	public class InventoryItemEventArgs : EventArgs
	{
		public InventoryItem InventoryItem;
	}

	public event EventHandler<OnInventoryUpdatedEventArgs> OnInventoryUpdated;
	public class OnInventoryUpdatedEventArgs : EventArgs
	{
		public List<InventoryItem> InventoryItems;
	}

	[field: SerializeField] public ItemDataSO CurrencyItem { get; private set; }
	[SerializeField] private int _slotAmount;
	[SerializeField] private float _timeBetweenCollections = 0.1f, _itemDropForce = 3f, _startingItemZAxis = 0.5f;
	[SerializeField] private ItemCollectWorldUI _itemCollectPlatePrefab;
	
	private InventoryModel _inventoryModel;
	private MouseItemModel _mouseItemModel;
	private Queue<InventoryItem> _itemQueue = new();
	private Dictionary<string, ItemCollectWorldUI> _itemPlates = new(); // Maybe replace string with an item id if I decide to make that later
	private bool _gotItemThisFrame, _gaveItemThisFrame, _isCollecting;
	
	private void Awake()
	{
		Instance = this;
		
		_mouseItemModel = new();
		_inventoryModel = new(_slotAmount);
		_inventoryModel.OnInventoryUpdate += InventoryModel_OnInventoryUpdate;
	}
	
	private void Start()
	{
		GameInput.Instance.OnSecondaryActionStarted += DropMouseItem;
	}

	private void Update()
	{
		if (_mouseItemModel.MouseInventoryItem.HasItem)
		{
			if (_gotItemThisFrame) return;

			UpdateMouseItem();

			_gotItemThisFrame = true;
			_gaveItemThisFrame = false;

			MOUSE_HAS_ITEM = true;
			Tooltip.HideUI();
		}
		else if (!_gaveItemThisFrame)
		{
			UpdateMouseItem();

			_gaveItemThisFrame = true;
			_gotItemThisFrame = false;

			MOUSE_HAS_ITEM = false;
		}
	}

	private void DropMouseItem(object sender, EventArgs e)
    {
        if(Pointer.IsOverUI() || Pointer.IsOverInteractable() || !_mouseItemModel.MouseInventoryItem.HasItem) return;

		GameManager.Instance.SpawnItem(_mouseItemModel.MouseInventoryItem, 
        Player.Instance.transform.position, 
        Player.Instance.CurrentBiome.Value, 
        ActionManager.PlayerToMouseDirNormalized * _itemDropForce,
        _startingItemZAxis);
        
		_mouseItemModel.MouseInventoryItem = new();
		_inventoryModel.UpdateInventory();
	}
	
	public bool SelectedItemExists(out InventoryItem selectedItem)
	{
		if(_mouseItemModel.MouseInventoryItem.HasItem)
		{
			selectedItem = _mouseItemModel.MouseInventoryItem;
			return _mouseItemModel.MouseInventoryItem.Item != null;
		}
		else
		{
			selectedItem = _inventoryModel.InventoryItems[GameInput.Instance.GetSelectedSlotIndex()];
			return _inventoryModel.InventoryItems[GameInput.Instance.GetSelectedSlotIndex()].Item != null;
		}
	}
	
	private void OnProjectileShot_UpdateInventory(object sender, EventArgs e)
	{
		_inventoryModel.UpdateInventory();
	}
	
	public void UpdateMouseItem()
	{
		OnMouseItemUpdated?.Invoke(this, new InventoryItemEventArgs
		{
			InventoryItem = _mouseItemModel.MouseInventoryItem
		});
	}
	
	public void RemoveItems(List<InventoryItem> items)
	{
		foreach (InventoryItem item in items)
		{
			_inventoryModel.RemoveItem(item.Item, item.Quantity);
		}
	}
	
	public void RemoveItem(ItemDataSO item, int amount)
	{
		_mouseItemModel.TryToRemoveItem(item, amount, out int remainder);
		
		if(remainder > 0)
		{
			_inventoryModel.RemoveItem(item, amount);
		}
		
		_inventoryModel.UpdateInventory();
	}
	
	public void AddItem(InventoryItem inventoryItem, bool playCollectSound = true)
	{
		// NTFS: BUG: This will create a brand new inventory item and will not transfer over any inventory item data that might have existed before
		_itemQueue.Enqueue(inventoryItem);
		
		if(!_isCollecting)
		{
			StartCoroutine(StaggeredItemCollection(playCollectSound));
		}
	}
	
	public void AddItem(ItemDataSO ItemToAdd, int quantity, bool playCollectSound = true)
	{
		// NTFS: BUG: This will create a brand new inventory item and will not transfer over any inventory item data that might have existed before
		_itemQueue.Enqueue(ItemToAdd.CreateInventoryItem(quantity));
		
		if(!_isCollecting)
		{
			StartCoroutine(StaggeredItemCollection(playCollectSound));
		}
	}
	
	private IEnumerator StaggeredItemCollection(bool playCollectSound)
	{
		_isCollecting = true;
		
		while(_itemQueue.Count > 0)
		{
			InventoryItem itemToCollect = _itemQueue.Dequeue();
			
			if (itemToCollect.Item == CurrencyItem)
			{
				GoldManager.Instance.AddGold(itemToCollect.Quantity);
				if (playCollectSound)
				{
					SoundManager.Instance.PlayOneShot(FMODEvents.Instance.GoldPickup, Player.Instance.transform.position);
				}
			}
			else
			{ 
				_inventoryModel.AddItem(itemToCollect);
				if (playCollectSound)
				{
					SoundManager.Instance.PlayOneShot(FMODEvents.Instance.ItemPickup, Player.Instance.transform.position);
				}
			}
			
			string itemName = itemToCollect.Item.InGameName;
			InventoryItem invItemToDisplay = new(itemToCollect.Item, itemToCollect.Quantity);

			// If there exists an item collect plate as the item being collected, delete it and spawn a new one
			if (_itemPlates.ContainsKey(itemName))
			{
				// Create refreshed item with updated quantities
				int currentQuantity = _itemPlates[itemName].DisplayAmount;
				int additionalQuantity = itemToCollect.Quantity;

				invItemToDisplay.Quantity = currentQuantity + additionalQuantity;
			
				// Delete the currently spawned item,
				Destroy(_itemPlates[itemName].gameObject);
			
				// Remove it from the dictionary
				_itemPlates.Remove(itemName);
			}

			SpawnItemCollectPlate(invItemToDisplay);
			
			yield return new WaitForSeconds(_timeBetweenCollections);
		}
		
		_isCollecting = false;
	}
	
	private void SpawnItemCollectPlate(InventoryItem itemToCollect)
	{
		string itemName = itemToCollect.Item.InGameName;
		ItemCollectWorldUI itemPlate = Instantiate(_itemCollectPlatePrefab, Player.Instance.transform.position, Quaternion.identity);
		itemPlate.DisplayedItem = itemToCollect;
		itemPlate.OnAnimationComplete += () => 
		{
			Destroy(itemPlate.gameObject);
			_itemPlates.Remove(itemName);
		};
		_itemPlates.Add(itemName, itemPlate);
	}
	
	public bool HasAllIngredients(List<InventoryItem> recipe)
	{
		if(recipe == null) return false;
	
		foreach (InventoryItem ingredient in recipe)
		{
			int inventoryAmount = _inventoryModel.GetAmount(ingredient.Item);
			int requiredAmount = ingredient.Quantity;
			
			if (inventoryAmount < requiredAmount)
			{
				return false;
			}
		}
		
		return true;
	}
	
	public void TryToCraft(RecipeSO recipeSO)
	{
		InventoryItem mouseItem = _mouseItemModel.MouseInventoryItem;

		// If mouse has an item that is not the recipe output, or mouse has an item that is not stackable, return
		if(mouseItem.HasItem)
		{
			// NOTE to future self: handle stack limits if you decide to have one
			if(!mouseItem.Item.Stackable || mouseItem.Item.InGameName != recipeSO.OutputItem.InGameName) return; 
			
			_mouseItemModel.MouseInventoryItem.Quantity += recipeSO.OutputAmount;
			
			// If mouse has no item, check if player has enough ingredients to craft the recipe
			RemoveItems(recipeSO.ResourceList);
		}
		else
		{
			// Add the output item to the mouse 
			_mouseItemModel.MouseInventoryItem.Item = recipeSO.OutputItem;
			_mouseItemModel.MouseInventoryItem.Quantity += recipeSO.OutputAmount;
			
			// If mouse has no item, check if player has enough ingredients to craft the recipe
			RemoveItems(recipeSO.ResourceList);
		}
	}
	
	private void InventoryModel_OnInventoryUpdate(List<InventoryItem> items)
	{
		UpdateMouseItem();
	
		OnInventoryUpdated?.Invoke(this, new OnInventoryUpdatedEventArgs
		{
			InventoryItems = items
		});
	}
	
	public void InventorySlotRightClicked(int clickedInventorySlotIndex, List<InventoryItem> inventory)
	{
		if(GameInput.Instance.GetShiftHeldDown())
		{
			OnInventorySlotShiftRightClicked?.Invoke(this, new ShortCutInventoryItemEventArgs
			{
				InventoryItem = inventory[clickedInventorySlotIndex],
				SlotIndex = clickedInventorySlotIndex
			});
			return;
		}
		
		InventoryItem inventoryItem = inventory[clickedInventorySlotIndex];
		InventoryItem mouseItem = _mouseItemModel.MouseInventoryItem;
		
		if(inventoryItem.HasItem)
		{
			if(mouseItem.HasItem) // Normal functionality
			{
				if(inventoryItem.Item.InGameName == mouseItem.Item.InGameName)
				{
					inventory[clickedInventorySlotIndex].Quantity += 1;
					_mouseItemModel.MouseInventoryItem.Quantity -= 1;
					
					if(_mouseItemModel.MouseInventoryItem.Quantity <= 0)
					{
						_mouseItemModel = new();
					}
				}
				else
				{
					// Swap the two items
					InventoryItem tempItem = inventoryItem;

					inventory[clickedInventorySlotIndex] = mouseItem;
					_mouseItemModel.MouseInventoryItem = tempItem;
				}
			}
			else
			{
				int inventoryItemQuantity = inventoryItem.Quantity;
				int newInventoryItemQuantity = inventoryItemQuantity / 2;
				int newMouseItemQuantity = inventoryItemQuantity - newInventoryItemQuantity;

				inventory[clickedInventorySlotIndex].Quantity = newInventoryItemQuantity;
				
				_mouseItemModel.MouseInventoryItem.Item = inventoryItem.Item;
				_mouseItemModel.MouseInventoryItem.Quantity = newMouseItemQuantity;
				
				if(inventoryItem.Quantity == 0)
				{

					inventory[clickedInventorySlotIndex].Item = null;
				}
				
				Tooltip.HideUI();
			}
		}
		else
		{
			if(mouseItem.HasItem)
			{

				inventory[clickedInventorySlotIndex].Item = mouseItem.Item;
				inventory[clickedInventorySlotIndex].Quantity = 1;
				
				_mouseItemModel.MouseInventoryItem.Quantity -= 1;
				if(_mouseItemModel.MouseInventoryItem.Quantity <= 0)
				{
					_mouseItemModel = new();

					ShowInventoryItemTooltip(_mouseItemModel.MouseInventoryItem);
				}
			}
		}
		
		// Play click feedbacks and update Inventory
		_inventoryModel.UpdateInventory();
		OnInventorySlotClicked?.Invoke(this, EventArgs.Empty);
		PlayClickFeedbacks();
	}
	
	public void InventorySlotLeftClicked(int clickedInventorySlotIndex, List<InventoryItem> inventory)
	{
		if(GameInput.Instance.GetShiftHeldDown())
		{
			OnInventorySlotShiftLeftClicked?.Invoke(this, new ShortCutInventoryItemEventArgs
			{
				InventoryItem = inventory[clickedInventorySlotIndex],
				SlotIndex = clickedInventorySlotIndex
			});

			_inventoryModel.UpdateInventory();
			OnInventorySlotClicked?.Invoke(this, EventArgs.Empty);
			return;
		}
		
		InventoryItem inventoryItem = inventory[clickedInventorySlotIndex];
		InventoryItem mouseItem = _mouseItemModel.MouseInventoryItem;
		
		if(inventoryItem.HasItem)
		{
			if(mouseItem.HasItem)
			{
				if(inventoryItem.Item.InGameName == mouseItem.Item.InGameName && mouseItem.Item.Stackable)
				{
					inventory[clickedInventorySlotIndex].Quantity += mouseItem.Quantity;
					_mouseItemModel.MouseInventoryItem = new();
					
					ShowInventoryItemTooltip(_mouseItemModel.MouseInventoryItem);
				}
				else
				{
					// Swap the two items
					InventoryItem tempItem = inventoryItem;

					inventory[clickedInventorySlotIndex] = mouseItem;
					_mouseItemModel.MouseInventoryItem = tempItem;
				}
			}
			else
			{
				_mouseItemModel.MouseInventoryItem = inventoryItem;
				inventory[clickedInventorySlotIndex] = new();
				
				Tooltip.HideUI();
			}
		}
		else
		{
			if(mouseItem.HasItem)
			{
				inventory[clickedInventorySlotIndex] = mouseItem;
				_mouseItemModel.MouseInventoryItem = new();
				
				ShowInventoryItemTooltip(inventory[clickedInventorySlotIndex]);
			}
		}
		
		// Update views and play click feedbacks
		PlayClickFeedbacks();
		_inventoryModel.UpdateInventory();
		OnInventorySlotClicked?.Invoke(this, EventArgs.Empty);
	}
	
	public void ShowInventoryItemTooltip(InventoryItem inventoryItem)
	{
		if(!inventoryItem.HasItem)
		{
			Debug.LogWarning($"Trying to display an inventory item that does not exists for {inventoryItem}");
			return;
		}
	
		Tooltip.ShowNew();

		switch (inventoryItem)
		{
			case WandInventoryItem wandInventoryItem:
				SpellItemSO[] magicArray = wandInventoryItem.MagicArray;
				Tooltip.WandDisplay(wandInventoryItem.Item as WandItemSO, magicArray, fontSize: 12f);
				break;
			default:	
				if(inventoryItem.Item is SpellItemSO spellItemSO)
				{
					Tooltip.SpellDisplay(spellItemSO, fontSize: 12f);
				}
				else
				{
					string quantityString = inventoryItem.Quantity > 1 ? $"[{inventoryItem.Quantity}]" : string.Empty;
					string itemText = $"{inventoryItem.Item.InGameName} {quantityString}<br>{inventoryItem.Item.GetDescription()}";

					Tooltip.JustText(itemText, Color.white, fontSize: 12f);
				}
				break;
		}
	}
	
	public void PlayClickFeedbacks()
	{
		SoundManager.Instance.PlayOneShot(FMODEvents.Instance.InventorySlotClicked, Player.Instance.transform.position);
	}
	
	public InventoryModel GetInventoryModel()
	{
		return _inventoryModel;
	}
	
	public MouseItemModel GetMouseItem()
	{
		return _mouseItemModel;
	}
	
	private void OnDestroy()
	{
		_inventoryModel.OnInventoryUpdate -= InventoryModel_OnInventoryUpdate;

		GameInput.Instance.OnSecondaryActionStarted -= DropMouseItem;
	}
}
