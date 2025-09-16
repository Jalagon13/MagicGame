using System;
using AdvancedTooltips.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ArmorSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private ArmorType _armorType; // The type of armor this slot represents (e.g., Head, Chest, Legs)
	[SerializeField] private Image _armorItemIcon; // The UI icon for the equipped armor
	[SerializeField] private Image _armorIcon;

	private ArmorItemSO _armorEquipped; // The currently equipped armor

    public void OnPointerClick(PointerEventData eventData)
	{
		// Get the item currently held by the mouse (if any)
		InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;

		if (ArmorEquipped())
		{
			// If there's already armor equipped in this slot
			if (mouseItem.HasItem)
			{
				if(mouseItem.Item is ArmorItemSO mouseArmorItem && mouseArmorItem.ArmorType == _armorType)
				{
					// Swap the equipped armor with the armor held by the mouse
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = SwapArmor(mouseArmorItem);
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = 1;
				}
			}
			else
			{
				// If no armor is held by the mouse, unequip the current armor
				if (GameInput.Instance.GetShiftHeldDown())
				{
					// If Shift is held, add the unequipped armor to the inventory
					InventoryManager.Instance.AddItem(UnequipArmor(), 1);
				}
				else
				{
					// Otherwise, place the unequipped armor on the mouse
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = UnequipArmor();
				}

				Tooltip.HideUI();
			}
		}
		else if (mouseItem.HasItem && mouseItem.Item is ArmorItemSO mouseArmorItem && mouseArmorItem.ArmorType == _armorType)
		{
			// If no armor is equipped and the mouse is holding armor, equip it
			EquipArmor(mouseArmorItem);
			InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
			InventoryManager.Instance.ShowInventoryItemTooltip(new InventoryItem(_armorEquipped, 1));
		}
		
		UpdateSlotUI();

		// Notify the inventory system to update
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}

	public void EquipArmor(ArmorItemSO armorItem)
	{
		// Equip the armor and update the reference
		_armorEquipped = armorItem;

		// NTFS: Need to incorporate method for custom special buffs for full armor sets being worn
		Buff defenseBuff = new(Player.Instance.ServerCharacter.Stats.Defense, new StatModifier(_armorEquipped.DefenseAmount, StatModifierType.Flat, this, true));
		Player.Instance.ServerCharacter.Stats.AddBuff(defenseBuff);
		
		InventoryManager.Instance.PlayClickFeedbacks();
		UpdateSlotUI();
	}

	public ArmorItemSO UnequipArmor()
	{
		// Unequip the armor and return it
		ArmorItemSO unequippedArmor = _armorEquipped;
		_armorEquipped = null;

		// Optionally, remove stats or effects from the armor
		Player.Instance.ServerCharacter.Stats.RemoveBuffsFromSource(this);

		InventoryManager.Instance.PlayClickFeedbacks();
		UpdateSlotUI();
		
		return unequippedArmor;
	}

	public ItemDataSO SwapArmor(ArmorItemSO newArmor)
	{
		// Swap the currently equipped armor with the new armor
		ArmorItemSO oldArmor = _armorEquipped;
		EquipArmor(newArmor);
		
		return oldArmor;
	}

	public void UpdateSlotUI()
	{
		// Enable or disable the icon based on whether armor is equipped
		_armorItemIcon.enabled = ArmorEquipped();
		_armorIcon.enabled = !ArmorEquipped();

		if (ArmorEquipped())
		{
			// Update the icon to display the equipped armor's sprite
			_armorItemIcon.sprite = _armorEquipped.UiDisplay;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		// Show a tooltip if armor is equipped
		if (ArmorEquipped())
		{
			Tooltip.ShowNew();
			InventoryManager.Instance.ShowInventoryItemTooltip(new(_armorEquipped, 1));
		}
		else
		{
			Tooltip.HideUI();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		// Hide the tooltip
		Tooltip.HideUI();
	}

	public bool ArmorEquipped()
	{
		// Check if armor is currently equipped
		return _armorEquipped != null;
	}
}