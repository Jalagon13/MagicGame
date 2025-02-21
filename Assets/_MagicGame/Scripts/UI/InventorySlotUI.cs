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
	
	private void OnDisable()
	{
		if(_hovered)
		{
			// TooltipManager.Instance.Hide();
		}
	}
	
	public void OnPointerClick(PointerEventData eventData)
	{
		if(eventData.button == PointerEventData.InputButton.Left)
		{
			InventoryManager.Instance.InventorySlotLeftClicked(_inventoryIndex);
		}
		else if(eventData.button == PointerEventData.InputButton.Right)
		{
			InventoryManager.Instance.InventorySlotRightClicked(_inventoryIndex);
		}
	}
	
	public void SetInventoryIndex(int inventoryIndex)
	{
		_inventoryIndex = inventoryIndex;
		GetComponent<RectTransform>().localScale = Vector3.one;
	}

	public void UpdateView(InventoryItem item)
	{
		_item = item;
		if(item.Item != null)
		{
			_itemImage.color = new Vector4(1, 1, 1, 1);
			_itemImage.sprite = item.Item.UiDisplay;
		}
		else
		{
			_itemImage.color = new Vector4(1, 1, 1, 0);
			_itemImage.sprite = null;
		}
		
		_itemQuantityText.text = item.Item != null ? item.Quantity > 1 ? item.Quantity.ToString() : string.Empty : string.Empty;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if(_item.HasItem && !InventoryManager.MOUSE_HAS_ITEM)
		{
			_hovered = true;
			InventoryItem inventoryItem = InventoryManager
			InventoryManager.Instance.ShowItemTooltip()
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if(_hovered)
		{
			_hovered = false;
			Tooltip.HideUI();
		}
	}

	public void ChangeColor(Color color)
	{
		GetComponent<Image>().color = color;
	}
}
