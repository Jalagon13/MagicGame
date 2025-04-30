using System;
using AdvancedTooltips.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellbookInventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	public Image SpellIcon;

	private SpellbookInventoryItem _spellbookInvItem;
	private int _spellIndex;

	public void Initialize(SpellbookInventoryItem selectedWand, int spellIndex)
	{
		_spellbookInvItem = selectedWand;
		_spellIndex = spellIndex;
		
		UpdateSlotUI();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;
		
		if(WandInventorySlotIsOccupied())
		{
			if(GameInput.Instance.GetShiftHeldDown())
			{
				InventoryManager.Instance.AddItem(_spellbookInvItem.RemoveMagic(_spellIndex), 1);
			}
			else if(mouseItem.HasItem && mouseItem.Item is SpellItemSO mouseMagicItemSO)
			{
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = _spellbookInvItem.SwapMagic(mouseMagicItemSO, _spellIndex);
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = 1;
			}
			else
			{
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = _spellbookInvItem.RemoveMagic(_spellIndex);
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = 1;
			}

			Tooltip.HideUI();
		}
		else if(mouseItem.HasItem && mouseItem.Item is SpellItemSO mouseMagicItemSO)
		{
			_spellbookInvItem.SetMagic(mouseMagicItemSO, _spellIndex);
			InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
			Tooltip.ShowNew();
			InventoryManager.Instance.ShowInventoryItemTooltip(_spellbookInvItem);
		}
		
		UpdateSlotUI();
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}
	
	public void SetMagic(SpellItemSO magicItem)
	{
		_spellbookInvItem.SetMagic(magicItem, _spellIndex);
		UpdateSlotUI();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if(_spellbookInvItem.MagicArray[_spellIndex] != null)
		{
			Tooltip.ShowNew();
			InventoryManager.Instance.ShowInventoryItemTooltip(_spellbookInvItem.MagicArray[_spellIndex].CreateInventoryItem(1));
		}
		else
		{
			Tooltip.HideUI();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Tooltip.HideUI();
	}
	
	public bool WandInventorySlotIsOccupied()
	{
		return _spellbookInvItem.MagicArray[_spellIndex] != null;
	}
	
	public void UpdateSlotUI()
	{
		if(WandInventorySlotIsOccupied())
		{
			SpellIcon.sprite = _spellbookInvItem.MagicArray[_spellIndex].SpellUIDisplaySprite;
			SpellIcon.color = new(1,1,1,1);
		}
		else
		{
			SpellIcon.sprite = null;
			SpellIcon.color = new(1,1,1,0);
		}
	}
}
