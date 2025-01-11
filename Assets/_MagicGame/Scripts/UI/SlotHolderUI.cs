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
	[SerializeField] private AudioClip _inventoryEnabledClip;
	[SerializeField] private AudioClip _inventoryDisabledClip;
	
	private List<InventorySlotUI> _inventorySlotUIList = new();
	
	private void Start()
	{
		GameInput.Instance.OnInventoryToggle += GameInput_OnInventoryToggle;
		InventoryManager.Instance.OnInventoryUpdated += InventoryManager_OnInventoryUpdated;
		
		Initialize(InventoryManager.Instance.GetInventoryModel().InventoryItems);
		
		HideInventorySlots();
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
		
		MMSoundManagerSoundPlayEvent.Trigger(_inventoryEnabledClip, MMSoundManager.MMSoundManagerTracks.UI, default);
	}
	
	private void HideInventorySlots()
	{
		_inventorySlotsUITransform.gameObject.SetActive(false);
		
		MMSoundManagerSoundPlayEvent.Trigger(_inventoryDisabledClip, MMSoundManager.MMSoundManagerTracks.UI, default);
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnInventoryToggle -= GameInput_OnInventoryToggle;
	}
}
