using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private Image _itemImage;
	[SerializeField] private Image _wandFocusImage;
	[SerializeField] private TextMeshProUGUI _itemQuantityText;
	
	private InventoryItem _item;
	private int _inventoryIndex;
	private bool _hovered;
	
	private void OnDisable()
	{
		if(_hovered)
		{
			TooltipManager.Instance.Hide();
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
			
			// if(item is SimpleWandInventoryItem simpleWandInventoryItem)
			// {
			// 	ItemSO wandFocusItemSO = simpleWandInventoryItem.ProjectileItemSO;
			// 	if(wandFocusItemSO != null)
			// 	{
			// 		_wandFocusImage.color = new Vector4(1, 1, 1, 1);
			// 		_wandFocusImage.sprite = wandFocusItemSO.UiDisplay;
			// 	}
			// 	else
			// 	{
			// 		_wandFocusImage.color = new Vector4(1, 1, 1, 0);
			// 		_wandFocusImage.sprite = null;
			// 	}
			// }
			
			_wandFocusImage.color = new Vector4(1, 1, 1, 0);
			_wandFocusImage.sprite = null;
		}
		else
		{
			_itemImage.color = new Vector4(1, 1, 1, 0);
			_itemImage.sprite = null;
			_wandFocusImage.color = new Vector4(1, 1, 1, 0);
			_wandFocusImage.sprite = null;
		}
		
		_itemQuantityText.text = item.Item != null ? item.Quantity > 1 ? item.Quantity.ToString() : string.Empty : string.Empty;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_hovered = true;
		if(_item.HasItem)
		{
			TooltipManager.Instance.Show(_item is WandInventoryItem wandItem ? wandItem.GetDescription() : _item.Item.GetDescription(), _item.Item.Name);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_hovered = false;
		TooltipManager.Instance.Hide();
	}

	public void ChangeColor(Color color)
	{
		GetComponent<Image>().color = color;
	}
}
