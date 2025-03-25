using System.Collections;
using System.Collections.Generic;
using AdvancedTooltips.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private Image _itemImage;
	[SerializeField] private TextMeshProUGUI _itemQuantityText;
	
	private InventoryItem _item;
	private int _inventoryIndex;
	private bool _hovered;
	private List<InventoryItem> _inventoryAssociatedWith;


	private void OnDisable()
	{
		if(_hovered)
		{
			Tooltip.HideUI();
		}
	}
	
	public void OnPointerClick(PointerEventData eventData)
	{
		if(eventData.button == PointerEventData.InputButton.Left)
		{
			InventoryManager.Instance.InventorySlotLeftClicked(_inventoryIndex, _inventoryAssociatedWith);
		}
		else if(eventData.button == PointerEventData.InputButton.Right)
		{
			if(_item.HasItem && _item is WandInventoryItem wandInventoryItem)
			{
				_inventoryAssociatedWith[_inventoryIndex] = new();

				InGameMenu.Instance.OpenWandInspectorMenu(wandInventoryItem);
			}
			else
			{
				InventoryManager.Instance.InventorySlotRightClicked(_inventoryIndex, _inventoryAssociatedWith);
			}
		}
	}
	
	public void InitializeInvSlotUI(int inventoryIndex, List<InventoryItem> inventoryAssociatedWith)
	{
		_inventoryAssociatedWith = inventoryAssociatedWith;
		_inventoryIndex = inventoryIndex;
		GetComponent<RectTransform>().localScale = Vector3.one;
	}

	public void UpdateDisplayUI(InventoryItem item)
	{
		_item = item;
		if(item.Item != null)
		{
			_itemImage.color = new Vector4(1, 1, 1, 1);
			_itemImage.sprite = item.Item.UiDisplay;

			_itemQuantityText.text = item.Quantity > 1 ? item.Quantity.ToString() : string.Empty;
		}
		else
		{
			_itemImage.color = new Vector4(1, 1, 1, 0);
			_itemImage.sprite = null;
			_itemQuantityText.text = string.Empty;
		}
	}
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		if (_item.HasItem && !InventoryManager.MOUSE_HAS_ITEM)
		{
			_hovered = true;
			
			Tooltip.ShowNew();

			switch (_item.Item)
			{
				
				case WandItemSO wandItemSO:
					MagicItemSO[] magicArray = (_inventoryAssociatedWith[_inventoryIndex] as WandInventoryItem).MagicArray;
					Tooltip.WandDisplay(wandItemSO, magicArray, fontSize: 12f);
					break;
				case SpellItemSO spellItemSO:
					Tooltip.SpellDisplay(spellItemSO, fontSize: 12f);
					break;
				default:
					int quantity = _inventoryAssociatedWith[_inventoryIndex].Quantity;
					string quantityString = quantity > 1 ? $"[{quantity}]" : string.Empty;
					string itemText = $"{_item.Item.Name} {quantityString}<br>Value: {_item.Item.GoldValue} Gold<br>{_item.Item.GetDescription()}";
					
					Tooltip.JustText(itemText, Color.white, fontSize: 12f);
					break;
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Tooltip.HideUI();
	}

	public void ChangeColor(Color color)
	{
		GetComponent<Image>().color = color;
	}
}
