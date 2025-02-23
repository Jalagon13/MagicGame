using System;
using AdvancedTooltips.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChestSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private Image _itemImage;
	[SerializeField] private TextMeshProUGUI _itemQuantityText;

	private ChestItemData _chestSlotItemData;
	private int _slotIndex;

	public void OnPointerClick(PointerEventData eventData)
	{
		if(eventData.button == PointerEventData.InputButton.Left)
		{
			if (GameInput.Instance.GetShiftHeldDown())
			{
				if(_chestSlotItemData != null)
				{
					InventoryManager.Instance.AddItem(GameManager.Instance.GetItemSOFromItemId(_chestSlotItemData.ItemId), _chestSlotItemData.Quantity);
					ChestManager.Instance.RemoveChestItemEntry(_slotIndex); // NTFS: No checks for if inventory is full or not
				}
			}
			else
			{
				ChestManager.Instance.ChestSlotLeftClicked(_slotIndex);
			}
		}
		else if(eventData.button == PointerEventData.InputButton.Right)
		{
			if (GameInput.Instance.GetShiftHeldDown())
			{
				if(_chestSlotItemData != null)
				{
					InventoryManager.Instance.AddItem(GameManager.Instance.GetItemSOFromItemId(_chestSlotItemData.ItemId), _chestSlotItemData.Quantity);
					ChestManager.Instance.RemoveChestItemEntry(_slotIndex); // NTFS: No checks for if inventory is full or not
				}
			}
			else
			{
				ChestManager.Instance.ChestSlotRightClicked(_slotIndex);
			}
		}
		
		ChestManager.Instance.UpdateChestSlots();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (_chestSlotItemData != null && !InventoryManager.MOUSE_HAS_ITEM)
		{
			ItemSO item = GameManager.Instance.GetItemSOFromItemId(_chestSlotItemData.ItemId);

			Tooltip.ShowNew();

			switch (item)
			{
				case WandItemSO wandItemSO:
					// MagicItemSO[] magicArray = (InventoryManager.Instance.GetInventoryModel().InventoryItems[_inventoryIndex] as WandInventoryItem).MagicArray;
					// Tooltip.WandDisplay(wandItemSO, magicArray, fontSize: 12f);
					break;
				case SpellItemSO spellItemSO:
					Tooltip.SpellDisplay(spellItemSO, fontSize: 12f);
					break;
				default:
					int quantity = _chestSlotItemData.Quantity;
					string quantityString = quantity > 1 ? $"[{quantity}]" : string.Empty;
					string itemText = $"{item.Name} {quantityString}<br>{item.GetDescription()}";

					Tooltip.JustText(itemText, Color.white, fontSize: 12f);
					break;
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Tooltip.HideUI();
	}

	public void UpdateChestSlot(ChestItemData itemData, int slotIndex)
	{
		_slotIndex = slotIndex;
		_chestSlotItemData = itemData;
		
		if(_chestSlotItemData != null)
		{
			_itemImage.color = new Vector4(1, 1, 1, 1);
			_itemImage.sprite = GameManager.Instance.GetItemSOFromItemId(_chestSlotItemData.ItemId).UiDisplay;
		}
		else
		{
			_itemImage.color = new Vector4(1, 1, 1, 0);
			_itemImage.sprite = null;
		}
		
		_itemQuantityText.text = _chestSlotItemData != null ? _chestSlotItemData.Quantity > 1 ? _chestSlotItemData.Quantity.ToString() : string.Empty : string.Empty;
	}
	
	private bool SlotHasItem()
	{
		return _chestSlotItemData != null;
	}
}
