using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellBookInventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private Image _spellIcon;

	// private SpellBookInventoryItem _spellBookInventoryItemRef;
	private int _spellIndex;

	// public void Initialize(SpellBookInventoryItem selectedSpellBook, int spellIndex)
	// {
	// 	_spellBookInventoryItemRef = selectedSpellBook;
	// 	_spellIndex = spellIndex;
		
	// 	UpdateSlotUI();
	// }

	public void OnPointerClick(PointerEventData eventData)
	{
		// InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;
		
		// if(SpellBookInventorySlotIsOccupied())
		// {
		// 	if(GameInput.Instance.GetShiftHeldDown())
		// 	{
		// 		InventoryManager.Instance.AddItem(_spellBookInventoryItemRef.RemoveSpell(_spellIndex), 1);
		// 	}
		// 	else if(mouseItem.HasItem && mouseItem.Item is SpellProjectileItemSO mouseSpellProjectileItemSO)
		// 	{
		// 		InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = _spellBookInventoryItemRef.SwapSpells(mouseSpellProjectileItemSO, _spellIndex);
		// 		InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = 1;
		// 	}
		// 	else
		// 	{
		// 		InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = _spellBookInventoryItemRef.RemoveSpell(_spellIndex);
		// 		InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = 1;
		// 	}
		// }
		// else if(mouseItem.HasItem && mouseItem.Item is SpellProjectileItemSO mouseSpellProjectileItemSO)
		// {
		// 	_spellBookInventoryItemRef.SetSpell(_spellIndex, mouseSpellProjectileItemSO);
		// 	InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
		// }
		
		// UpdateSlotUI();
		
		// InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}
	
	public void SetSpell(SpellProjectileItemSO spell)
	{
		// _spellBookInventoryItemRef.SetSpell(_spellIndex, spell);
		// UpdateSlotUI();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		
	}
	
	// public bool SpellBookInventorySlotIsOccupied()
	// {
	// 	return _spellBookInventoryItemRef.SpellsArray[_spellIndex] != null;
	// }
	
	// public void UpdateSlotUI()
	// {
	// 	if(SpellBookInventorySlotIsOccupied())
	// 	{
	// 		_spellIcon.sprite = _spellBookInventoryItemRef.SpellsArray[_spellIndex].UiDisplay;
	// 		_spellIcon.color = new(1,1,1,1);
	// 	}
	// 	else
	// 	{
	// 		_spellIcon.sprite = null;
	// 		_spellIcon.color = new(1,1,1,0);
	// 	}
	// }
}
