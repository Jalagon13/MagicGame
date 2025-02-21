using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
	public static InventoryManager Instance { get; private set; }
	public static int HOTBAR_SLOTS_AMOUNT = 9;
	
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

	
	[SerializeField] private int _slotAmount;
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
	
	private void Update()
	{
		MouseItemInput();
	}
	
	// NTFS: Refactor this later into a non update input check might be buggy
	private void MouseItemInput()
	{
		if (_mouseItemModel.MouseInventoryItem.HasItem)
		{
			if (_gotItemThisFrame) return;

			UpdateMouseItem();
			
			_gotItemThisFrame = true;
			_gaveItemThisFrame = false;
		}
		else if (!_gaveItemThisFrame)
		{
			UpdateMouseItem();
			
			_gaveItemThisFrame = true;
			_gotItemThisFrame = false;
		}
	}
	
	public bool MainHandItemExists(out InventoryItem mainHandInventoryItem)
	{
		if(_mouseItemModel.MouseInventoryItem.HasItem)
		{
			mainHandInventoryItem = _mouseItemModel.MouseInventoryItem;
			return _mouseItemModel.MouseInventoryItem.Item != null;
		}
		else
		{
			mainHandInventoryItem = _inventoryModel.InventoryItems[GameInput.Instance.GetSelectedSlotIndex()];
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
	
	public void RemoveItem(ItemSO item, int amount)
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
	
	public void AddItem(ItemSO ItemToAdd, int quantity, bool playCollectSound = true)
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
			if(playCollectSound)
			{
				SoundManager.Instance.PlayOneShot(FMODEvents.Instance.ItemPickup, Player.LocalClientInstance.transform.position);
			}
		
			InventoryItem itemToCollect = _itemQueue.Dequeue();
			
			_inventoryModel.AddItem(itemToCollect);
		
			string itemName = itemToCollect.Item.Name;
		
			// If there exists an item collect plate as the item being collected, delete it and spawn a new one
			if(_itemPlates.ContainsKey(itemName))
			{
				// Create refreshed item with updated quantities
				int currentQuantity = _itemPlates[itemName].DisplayAmount;
				int additionalQuantity = itemToCollect.Quantity;
			
				itemToCollect.Quantity = currentQuantity + additionalQuantity;
			
				// Delete the currently spawned item,
				Destroy(_itemPlates[itemName].gameObject);
			
				// Remove it from the dictionary
				_itemPlates.Remove(itemName);
			}
		
			SpawnItemCollectPlate(itemToCollect);
			
			yield return new WaitForSeconds(0.15f);
		}
		
		_isCollecting = false;
	}
	
	private void SpawnItemCollectPlate(InventoryItem itemToCollect)
	{
		string itemName = itemToCollect.Item.Name;
		ItemCollectWorldUI itemPlate = Instantiate(_itemCollectPlatePrefab, Player.LocalClientInstance.transform.position, Quaternion.identity);
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
			if(!mouseItem.Item.Stackable || mouseItem.Item.Name != recipeSO.OutputItem.Name) return; 
			
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
	
	public void InventorySlotRightClicked(int clickedInventorySlotIndex)
	{
		if(GameInput.Instance.GetShiftHeldDown())
		{
			OnInventorySlotShiftRightClicked?.Invoke(this, new ShortCutInventoryItemEventArgs
			{
				InventoryItem = _inventoryModel.InventoryItems[clickedInventorySlotIndex],
				SlotIndex = clickedInventorySlotIndex
			});
			return;
		}
	
		InventoryItem inventoryItem = _inventoryModel.InventoryItems[clickedInventorySlotIndex];
		InventoryItem mouseItem = _mouseItemModel.MouseInventoryItem;
		
		if(inventoryItem.HasItem)
		{
			if(mouseItem.HasItem) // Normal functionality
			{
				if(inventoryItem.Item.Name == mouseItem.Item.Name)
				{
					_inventoryModel.InventoryItems[clickedInventorySlotIndex].Quantity += 1;
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
					
					_inventoryModel.InventoryItems[clickedInventorySlotIndex] = mouseItem;
					_mouseItemModel.MouseInventoryItem = tempItem;
				}
			}
			else
			{
				int inventoryItemQuantity = inventoryItem.Quantity;
				int newInventoryItemQuantity = inventoryItemQuantity / 2;
				int newMouseItemQuantity = inventoryItemQuantity - newInventoryItemQuantity;
				
				_inventoryModel.InventoryItems[clickedInventorySlotIndex].Quantity = newInventoryItemQuantity;
				
				_mouseItemModel.MouseInventoryItem.Item = inventoryItem.Item;
				_mouseItemModel.MouseInventoryItem.Quantity = newMouseItemQuantity;
				
				if(inventoryItem.Quantity == 0)
				{
					_inventoryModel.InventoryItems[clickedInventorySlotIndex].Item = null;
				}
				
				// TooltipManager.Instance.Hide();
			}
		}
		else
		{
			if(mouseItem.HasItem)
			{
				_inventoryModel.InventoryItems[clickedInventorySlotIndex].Item = mouseItem.Item;
				_inventoryModel.InventoryItems[clickedInventorySlotIndex].Quantity = 1;
				
				_mouseItemModel.MouseInventoryItem.Quantity -= 1;
				if(_mouseItemModel.MouseInventoryItem.Quantity <= 0)
				{
					_mouseItemModel = new();
					// TooltipManager.Instance.Show(mouseItem is SpellBookInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
				}
			}
		}
		
		// Play click feedbacks and update Inventory
		_inventoryModel.UpdateInventory();
		PlayClickFeedbacks();
	}
	
	public void InventorySlotLeftClicked(int clickedInventorySlotIndex)
	{
		if(GameInput.Instance.GetShiftHeldDown())
		{
			OnInventorySlotShiftLeftClicked?.Invoke(this, new ShortCutInventoryItemEventArgs
			{
				InventoryItem = _inventoryModel.InventoryItems[clickedInventorySlotIndex],
				SlotIndex = clickedInventorySlotIndex
			});
			return;
		}
	
		InventoryItem inventoryItem = _inventoryModel.InventoryItems[clickedInventorySlotIndex];
		InventoryItem mouseItem = _mouseItemModel.MouseInventoryItem;
		
		if(inventoryItem.HasItem)
		{
			if(mouseItem.HasItem)
			{
				if(inventoryItem.Item.Name == mouseItem.Item.Name && mouseItem.Item.Stackable)
				{
					_inventoryModel.InventoryItems[clickedInventorySlotIndex].Quantity += mouseItem.Quantity;
					_mouseItemModel.MouseInventoryItem = new();
					// TooltipManager.Instance.Show(mouseItem is SpellBookInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
				}
				else
				{
					// Swap the two items
					InventoryItem tempItem = inventoryItem;
					
					_inventoryModel.InventoryItems[clickedInventorySlotIndex] = mouseItem;
					_mouseItemModel.MouseInventoryItem = tempItem;
				}
			}
			else
			{
				_mouseItemModel.MouseInventoryItem = inventoryItem;
				_inventoryModel.InventoryItems[clickedInventorySlotIndex] = new();
				// TooltipManager.Instance.Hide();
			}
		}
		else
		{
			if(mouseItem.HasItem)
			{
				_inventoryModel.InventoryItems[clickedInventorySlotIndex] = mouseItem;
				_mouseItemModel.MouseInventoryItem = new();
				// TooltipManager.Instance.Show(mouseItem is SpellBookInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
			}
		}
		
		// Update views and play click feedbacks
		_inventoryModel.UpdateInventory();
		PlayClickFeedbacks();
	}
	
	private void PlayClickFeedbacks()
	{
		SoundManager.Instance.PlayOneShot(FMODEvents.Instance.InventorySlotClicked, Player.LocalClientInstance.transform.position);
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
	}
}
