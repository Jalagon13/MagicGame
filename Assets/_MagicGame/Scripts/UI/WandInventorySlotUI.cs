using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WandInventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private Image _spellIcon;

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
				InventoryManager.Instance.AddItem(_wandInvItem.RemoveSpell(_spellIndex), 1);
			}
			else if(mouseItem.HasItem && mouseItem.Item is SpellItemSO mouseSpellProjectileItemSO)
			{
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = _wandInvItem.SwapSpells(mouseSpellProjectileItemSO, _spellIndex);
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = 1;
			}
			else
			{
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = _wandInvItem.RemoveSpell(_spellIndex);
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = 1;
			}
		}
		else if(mouseItem.HasItem && mouseItem.Item is SpellItemSO mouseSpellProjectileItemSO)
		{
			_wandInvItem.SetSpell(mouseSpellProjectileItemSO, _spellIndex);
			InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
		}
		
		UpdateSlotUI();
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}
	
	public void SetSpell(SpellItemSO spell)
	{
		_wandInvItem.SetSpell(spell, _spellIndex);
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
		return _wandInvItem.SpellArray[_spellIndex] != null;
	}
	
	public void UpdateSlotUI()
	{
		if(WandInventorySlotIsOccupied())
		{
			_spellIcon.sprite = _wandInvItem.SpellArray[_spellIndex].UiDisplay;
			_spellIcon.color = new(1,1,1,1);
		}
		else
		{
			_spellIcon.sprite = null;
			_spellIcon.color = new(1,1,1,0);
		}
	}
}
