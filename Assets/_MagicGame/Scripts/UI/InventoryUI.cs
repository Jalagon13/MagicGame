using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedTooltips.Core;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
	[field: SerializeField] public GameObject ToggleInventorySlotsGO { get; private set; }
	[field: SerializeField] public Transform HotbarSlotsUITransform { get; private set; }
	[field: SerializeField] public Transform InventorySlotsUITransform { get; private set; }
	[field: SerializeField] public Transform ChestSlotsUITransform { get; private set; }

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
		ToggleInventorySlotsGO.SetActive(true);

		SoundManager.Instance.PlayOneShot(FMODEvents.Instance.InventoryOpen, Player.LocalClientInstance.transform.position);
	}

	private void Hide()
	{
		Tooltip.HideUI();
		ToggleInventorySlotsGO.SetActive(false);

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
		foreach (Transform child in ChestSlotsUITransform)
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
		foreach (Transform hotbarSlot in HotbarSlotsUITransform)
		{
			InventorySlotUI hotbarIntSlotUI = hotbarSlot.gameObject.GetComponent<InventorySlotUI>();
			hotbarIntSlotUI.InitializeInvSlotUI(indexCounter, InventoryManager.Instance.GetInventoryModel().InventoryItems);
			indexCounter++;

			_inventorySlotUIList.Add(hotbarIntSlotUI);
		}
		
		foreach (Transform invSlot in InventorySlotsUITransform)
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
		ChestSlotsUITransform.gameObject.SetActive(true);
	}
	
	private void HideChestSlots()
	{
		ChestSlotsUITransform.gameObject.SetActive(false);
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
