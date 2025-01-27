using System;
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
				InventoryManager.Instance.AddItem(GameManager.Instance.GetItemSOFromItemId(_chestSlotItemData.ItemId), _chestSlotItemData.Quantity);
				ChestManager.Instance.RemoveChestItemEntry(_slotIndex); // NTFS: No checks for if inventory is full or not
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
				InventoryManager.Instance.AddItem(GameManager.Instance.GetItemSOFromItemId(_chestSlotItemData.ItemId), _chestSlotItemData.Quantity);
				ChestManager.Instance.RemoveChestItemEntry(_slotIndex); // NTFS: No checks for if inventory is full or not
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
		
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		
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
