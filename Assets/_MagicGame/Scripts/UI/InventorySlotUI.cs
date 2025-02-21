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
		_hovered = true;
		if(_item.HasItem)
		{
			Tooltip.ShowNew();

			switch (_item.Item)
			{
				case WandItemSO wandItemSO:
					MagicItemSO[] magicArray = (InventoryManager.Instance.GetInventoryModel().InventoryItems[_inventoryIndex] as WandInventoryItem).MagicArray;
					Tooltip.WandDisplay(wandItemSO, magicArray, iconScale: 0.75f, fontSize: 12f);
					break;
				default:
					Tooltip.JustText(_item.Item.UiDisplay, Color.white, _item.Item.Name.ToString(), Color.white);
					break;
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_hovered = false;
		Tooltip.HideUI();
	}

	public void ChangeColor(Color color)
	{
		GetComponent<Image>().color = color;
	}
}
