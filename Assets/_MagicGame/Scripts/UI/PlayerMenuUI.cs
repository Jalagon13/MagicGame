using System;
using UnityEngine;

public class PlayerMenuUI : MonoBehaviour
{
	[SerializeField] private ArmorSlotUI _helmetArmorSlotUI;
	[SerializeField] private ArmorSlotUI _chestplateArmorSlotUI;
	[SerializeField] private ArmorSlotUI _leggingsArmorSlotUI;

	private void OnEnable()
	{
		InventoryManager.Instance.OnInventorySlotShiftLeftClicked += OnInventorySlotShiftLeftClicked_EquipShortcut;
	}

	private void OnDisable()
	{
		InventoryManager.Instance.OnInventorySlotShiftLeftClicked -= OnInventorySlotShiftLeftClicked_EquipShortcut;
	}

	private void OnInventorySlotShiftLeftClicked_EquipShortcut(object sender, InventoryManager.ShortCutInventoryItemEventArgs e)
	{
		if(ChestManager.Instance.IsChestOpen) return;
	
		if (e.InventoryItem.Item is ArmorItemSO armorItemSO)
		{
			switch (armorItemSO.ArmorType)
			{
				case ArmorType.Head:
					HandleArmorEquipOrSwap(_helmetArmorSlotUI, armorItemSO, e.SlotIndex);
					break;
				case ArmorType.Chest:
					HandleArmorEquipOrSwap(_chestplateArmorSlotUI, armorItemSO, e.SlotIndex);
					break;
				case ArmorType.Legs:
					HandleArmorEquipOrSwap(_leggingsArmorSlotUI, armorItemSO, e.SlotIndex);
					break;
			}
		}
	}

	private void HandleArmorEquipOrSwap(ArmorSlotUI armorSlotUI, ArmorItemSO armorItemSO, int slotIndex)
	{
		if (armorSlotUI.ArmorEquipped())
		{
			// If armor is already equipped, swap it with the new armor
			InventoryManager.Instance.GetInventoryModel().InventoryItems[slotIndex].Item = armorSlotUI.SwapArmor(armorItemSO);
			InventoryManager.Instance.GetInventoryModel().InventoryItems[slotIndex].Quantity = 1;
		}
		else
		{
			// If no armor is equipped, equip the new armor
			armorSlotUI.EquipArmor(armorItemSO);
			InventoryManager.Instance.GetInventoryModel().InventoryItems[slotIndex] = new();
		}
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}
}