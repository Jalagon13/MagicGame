using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedTooltips.Core;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventorySlotsUI : MonoBehaviour
{
	[SerializeField] private Transform _hotbarSlotsUITransform;
	[SerializeField] private Transform _inventorySlotsUITransform;
	[SerializeField] private Transform _chestSlotsUITransform;
	
	private List<InventorySlotUI> _inventorySlotUIList = new();
	
	private void Start()
	{
		GameInput.Instance.OnInventoryToggle += GameInput_OnInventoryToggle;
		InventoryManager.Instance.OnInventoryUpdated += InventoryManager_OnInventoryUpdated;
		ChestManager.Instance.OnChestOpen += ChestManager_OnChestOpen;
		ChestManager.Instance.OnChestClose += ChestManager_OnChestClose;
		ChestManager.Instance.OnChestUpdated += ChestManager_OnChestUpdated;
		
		InitializeSlots();
		Hide();
		HideChestSlots();
	}

	private void GameInput_OnInventoryToggle(object sender, GameInput.OnToggleInventoryEventArgs e)
	{
		if (e.InventoryOpen)
		{
			Show();
		}
		else
		{
			Hide();
		}
	}

	private void Show()
	{
		gameObject.SetActive(true);

		SoundManager.Instance.PlayOneShot(FMODEvents.Instance.InventoryOpen, Player.LocalClientInstance.transform.position);
	}

	private void Hide()
	{
		Tooltip.HideUI();
		gameObject.SetActive(false);

		if (Player.LocalClientInstance != null)
		{
			SoundManager.Instance.PlayOneShot(FMODEvents.Instance.InventoryClose, Player.LocalClientInstance.transform.position);
		}
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

	private void UpdateChestSlotDisplay(List<InventoryItem> chestItemData)
	{
		foreach (Transform child in _chestSlotsUITransform)
		{
			int chestSlotIndex = child.GetSiblingIndex();

			child.GetComponent<InventorySlotUI>().InitializeInvSlotUI(chestSlotIndex, ChestManager.Instance.GetOpenChestInventoryItems());
			child.GetComponent<InventorySlotUI>().UpdateDisplayUI(chestItemData[chestSlotIndex]);
		}
	}

	private void InventoryManager_OnInventoryUpdated(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
	{
		UpdateUI(e.InventoryItems);
	}

	public void InitializeSlots()
	{
		int indexCounter = 0;
		
		// For the first 9 inventory items, generate the slots as hotbar slots
		foreach (Transform hotbarSlot in _hotbarSlotsUITransform)
		{
			InventorySlotUI hotbarIntSlotUI = hotbarSlot.gameObject.GetComponent<InventorySlotUI>();
			hotbarIntSlotUI.InitializeInvSlotUI(indexCounter, InventoryManager.Instance.GetInventoryModel().InventoryItems);
			indexCounter++;

			_inventorySlotUIList.Add(hotbarIntSlotUI);
		}
		
		foreach (Transform invSlot in _inventorySlotsUITransform)
		{
			InventorySlotUI invSlotUI = invSlot.gameObject.GetComponent<InventorySlotUI>();
			invSlotUI.InitializeInvSlotUI(indexCounter, InventoryManager.Instance.GetInventoryModel().InventoryItems);
			indexCounter++;

			_inventorySlotUIList.Add(invSlotUI);
		}
	}
	
	public void UpdateUI(List<InventoryItem> updatedInventory)
	{
		for(int i = 0; i < _inventorySlotUIList.Count; i++)
		{
			InventorySlotUI isv = _inventorySlotUIList[i];
			InventoryItem inventoryItem = updatedInventory[i];
			
			isv.UpdateDisplayUI(inventoryItem);
		}
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
		InventoryManager.Instance.OnInventoryUpdated -= InventoryManager_OnInventoryUpdated;
	}
}
