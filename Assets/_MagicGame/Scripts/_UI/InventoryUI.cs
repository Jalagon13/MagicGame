using System;
using System.Collections;
using System.Collections.Generic;
using AdvancedTooltips.Core;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
	[field: SerializeField] public GameObject ToggleInventorySlotsGO { get; private set; }
	[field: SerializeField] public Transform HotbarSlotsUITransform { get; private set; }
	[field: SerializeField] public Transform InventorySlotsUITransform { get; private set; }

	private List<InventorySlotUI> _inventorySlotUIList = new();
	
	private void Start()
	{
		GameInput.Instance.OnInventoryToggle += GameInput_OnInventoryToggle;
		InventoryManager.Instance.OnInventoryUpdated += InventoryManager_OnInventoryUpdated;
		
		InitializeSlots();
		Hide();
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
	
	
	private void OnDestroy()
	{
		GameInput.Instance.OnInventoryToggle -= GameInput_OnInventoryToggle;
		InventoryManager.Instance.OnInventoryUpdated -= InventoryManager_OnInventoryUpdated;
	}
}
