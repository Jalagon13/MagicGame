using AdvancedTooltips.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WandSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private WandInspectorMenuUI _wandMenuUI;
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

			Tooltip.HideUI();
		}
		else if(mouseItem.HasItem && mouseItem is WandInventoryItem mouseWandInventoryItem)
		{
			_wandMenuUI.PlaceSelectedWand(mouseWandInventoryItem);
			InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
			InventoryManager.Instance.ShowInventoryItemTooltip(_wandMenuUI.SelectedWand);
		}
		
		UpdateSlotUI();
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_hovered = true;
		
		if(_wandMenuUI.HasWand())
		{
			Tooltip.ShowNew();
			InventoryManager.Instance.ShowInventoryItemTooltip(_wandMenuUI.SelectedWand);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_hovered = false;
		
		Tooltip.HideUI();
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
				Tooltip.ShowNew();
				InventoryManager.Instance.ShowInventoryItemTooltip(_wandMenuUI.SelectedWand);
			}
		}
	}
}
