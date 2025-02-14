using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WandSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private WandMenuUI _wandMenuUI;
	[SerializeField] private Image _backgroundIcon;
	[SerializeField] private Image _wandIcon;
	
	private bool _hovered;
	
	private void OnEnable()
	{
		UpdateSlotUI();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;
		
		if(_wandMenuUI.HasWand())
		{
			if(mouseItem.HasItem && mouseItem is WandInventoryItem mouseWandInventoryItem)
			{
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem = _wandMenuUI.SwapWands(mouseWandInventoryItem);
			}
			else
			{
				if(GameInput.Instance.GetShiftHeldDown())
				{
					InventoryManager.Instance.AddItem(_wandMenuUI.RemoveSelectedWand());
				}
				else
				{
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = _wandMenuUI.RemoveSelectedWand();
				}
			}
		}
		else if(mouseItem.HasItem && mouseItem is WandInventoryItem mouseWandInventoryItem)
		{
			_wandMenuUI.PlaceSelectedWand(mouseWandInventoryItem);
			InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
		}
		
		UpdateSlotUI();
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_hovered = true;
		
		if(_wandMenuUI.HasWand())
		{
			// TooltipManager.Instance.Show(_wandItem.GetDescription(), _wandItem.Item.Name);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_hovered = false;
		TooltipManager.Instance.Hide();
	}
	
	public void UpdateSlotUI()
	{
		_backgroundIcon.enabled = !_wandMenuUI.HasWand();
		_wandIcon.enabled = _wandMenuUI.HasWand();
		
		if(_wandMenuUI.HasWand())
		{
			_wandIcon.sprite = _wandMenuUI.SelectedWand.Item.UiDisplay;
			
			if(_hovered)
			{
				// TooltipManager.Instance.Show(wandItem.GetDescription(), wandItem.Item.Name);
			}
		}
	}
}
