using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WandInventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	public Image SpellIcon;

	private WandInventoryItem _wandInvItem;
	private int _spellIndex;

	public void Initialize(WandInventoryItem selectedWand, int spellIndex)
	{
		_wandInvItem = selectedWand;
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
				InventoryManager.Instance.AddItem(_wandInvItem.RemoveMagic(_spellIndex), 1);
			}
			else if(mouseItem.HasItem && mouseItem.Item is MagicItemSO mouseMagicItemSO)
			{
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = _wandInvItem.SwapMagic(mouseMagicItemSO, _spellIndex);
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = 1;
			}
			else
			{
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = _wandInvItem.RemoveMagic(_spellIndex);
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = 1;
			}
		}
		else if(mouseItem.HasItem && mouseItem.Item is MagicItemSO mouseMagicItemSO)
		{
			_wandInvItem.SetMagic(mouseMagicItemSO, _spellIndex);
			InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
		}
		
		UpdateSlotUI();
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}
	
	public void SetMagic(MagicItemSO magicItem)
	{
		_wandInvItem.SetMagic(magicItem, _spellIndex);
		UpdateSlotUI();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		
	}
	
	public bool WandInventorySlotIsOccupied()
	{
		return _wandInvItem.MagicArray[_spellIndex] != null;
	}
	
	public void UpdateSlotUI()
	{
		if(WandInventorySlotIsOccupied())
		{
			SpellIcon.sprite = _wandInvItem.MagicArray[_spellIndex].UiDisplay;
			SpellIcon.color = new(1,1,1,1);
		}
		else
		{
			SpellIcon.sprite = null;
			SpellIcon.color = new(1,1,1,0);
		}
	}
}
