using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

public class SlotHolderUI : MonoBehaviour
{
	[SerializeField] private InventorySlotUI _inventorySlotUIPrefab;
	[SerializeField] private Transform _hotbarSlotsUITransform;
	[SerializeField] private Transform _inventorySlotsUITransform;
	[SerializeField] private Transform _offHandSlotUITransform;
	[SerializeField] private Transform _chestSlotsUITransform;
	
	private List<InventorySlotUI> _inventorySlotUIList = new();
	
	private void Start()
	{
		GameInput.Instance.OnInventoryToggle += GameInput_OnInventoryToggle;
		InventoryManager.Instance.OnInventoryUpdated += InventoryManager_OnInventoryUpdated;
		ChestManager.Instance.OnChestOpen += ChestManager_OnChestOpen;
		ChestManager.Instance.OnChestClose += ChestManager_OnChestClose;
		ChestManager.Instance.OnChestUpdated += ChestManager_OnChestUpdated;
		
		Initialize(InventoryManager.Instance.GetInventoryModel().InventoryItems);
		
		HideInventorySlots();
		HideChestSlots();
	}

	private void ChestManager_OnChestUpdated(object sender, ChestManager.ChestEventArgs e)
	{
		UpdateChestSlotDisplay(e.ChestItemData);
	}

	private void ChestManager_OnChestClose(object sender, EventArgs e)
	{
		HideChestSlots();
	}

	private void ChestManager_OnChestOpen(object sender, ChestManager.ChestEventArgs e)
	{
		ShowChestSlots();
		UpdateChestSlotDisplay(e.ChestItemData);
	}

	private void UpdateChestSlotDisplay(List<ChestItemData> chestItemData)
	{
		foreach (Transform child in _chestSlotsUITransform)
		{
			int chestSlotIndex = child.GetSiblingIndex();
			bool foundItemForThisSlot = false;
			
			if(chestItemData.Count > 0)
			{
				foreach (ChestItemData itemData in chestItemData)
				{
					if(itemData.SlotIndex == chestSlotIndex)
					{
						// Found a chest item that should occupy this chest slot
						child.GetComponent<ChestSlotUI>().UpdateChestSlot(itemData, chestSlotIndex);
						foundItemForThisSlot = true;
						break;
					}
				}
				
				if(foundItemForThisSlot)
				{
					continue;
				}
			}
			
			// If could not find a chestItemData for this slot, initialize it as empty and continue
			child.GetComponent<ChestSlotUI>().UpdateChestSlot(null, chestSlotIndex);
		}
	}

	private void InventoryManager_OnInventoryUpdated(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
	{
		UpdateUI(e.InventoryItems);
	}

	private void GameInput_OnInventoryToggle(object sender, GameInput.OnToggleInventoryEventArgs e)
	{
		if(e.InventoryOpen)
		{
			ShowInventorySlots();
		}
		else
		{
			HideInventorySlots();
			HideChestSlots();
		}
	}

	public void Initialize(List<InventoryItem> inventoryItems)
	{
		// For the first 9 inventory items, generate the slots as hotbar slots
		for (int i = 0; i < inventoryItems.Count; i++)
		{
			// If one of the first 9 slots, add it to _hotbarSlotHolder, else, add it to _inventorySlotView
			if(i < 9)
			{
				InitializeSlot(_hotbarSlotsUITransform, i);
			}
			else if (i < inventoryItems.Count - 1)
			{
				InitializeSlot(_inventorySlotsUITransform, i);
			}
			else if(i == inventoryItems.Count - 1) // If last slot, initialize it as the off hand slot
			{
				InitializeSlot(_offHandSlotUITransform, i);
			}
		}
	}
	
	private void InitializeSlot(Transform slotHolder, int inventoryIndex)
	{
		InventorySlotUI invSlotUI = Instantiate(_inventorySlotUIPrefab, default, Quaternion.identity);
		invSlotUI.transform.SetParent(slotHolder);
		invSlotUI.SetInventoryIndex(inventoryIndex);
		
		_inventorySlotUIList.Add(invSlotUI);
	}
	
	public void UpdateUI(List<InventoryItem> updatedInventory)
	{
		for(int i = 0; i < _inventorySlotUIList.Count; i++)
		{
			InventorySlotUI isv = _inventorySlotUIList[i];
			InventoryItem inventoryItem = updatedInventory[i];
			
			isv.UpdateView(inventoryItem);
		}
	}
	
	private void ShowInventorySlots()
	{
		_inventorySlotsUITransform.gameObject.SetActive(true);
	}
	
	private void HideInventorySlots()
	{
		_inventorySlotsUITransform.gameObject.SetActive(false);
	}
	
	private void ShowChestSlots()
	{
		_chestSlotsUITransform.gameObject.SetActive(true);
	}
	
	private void HideChestSlots()
	{
		_chestSlotsUITransform.gameObject.SetActive(false);
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnInventoryToggle -= GameInput_OnInventoryToggle;
		ChestManager.Instance.OnChestOpen -= ChestManager_OnChestOpen;
		ChestManager.Instance.OnChestClose -= ChestManager_OnChestClose;
		ChestManager.Instance.OnChestUpdated -= ChestManager_OnChestUpdated;
	}
}
