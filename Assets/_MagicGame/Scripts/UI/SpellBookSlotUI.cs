using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellBookSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private SpellBookMenuUI _spellBookMenuUI;
	[SerializeField] private Image _backgroundIcon;
	[SerializeField] private Image _spellBookIcon;
	
	private bool _hovered;

	public void OnPointerClick(PointerEventData eventData)
	{
		InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;
		
		if(_spellBookMenuUI.HasSpellBook())
		{
			if(mouseItem.HasItem && mouseItem is SpellBookInventoryItem mouseSpellInventoryItem)
			{
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem = _spellBookMenuUI.SwapSpellBooks(mouseSpellInventoryItem);
			}
			else
			{
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem = _spellBookMenuUI.RemoveSelectedSpellBook();
			}
		}
		else if(mouseItem.HasItem && mouseItem is SpellBookInventoryItem mouseSpellInventoryItem)
		{
			_spellBookMenuUI.PlaceSelectedSpellBook(mouseSpellInventoryItem);
			InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
		}
		
		UpdateSlotUI();
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_hovered = true;
		
		if(_spellBookMenuUI.HasSpellBook())
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
		_backgroundIcon.enabled = !_spellBookMenuUI.HasSpellBook();
		_spellBookIcon.enabled = _spellBookMenuUI.HasSpellBook();
		
		if(_spellBookMenuUI.HasSpellBook())
		{
			_spellBookIcon.sprite = _spellBookMenuUI.SelectedSpellBook.Item.UiDisplay;
			
			if(_hovered)
			{
				// TooltipManager.Instance.Show(wandItem.GetDescription(), wandItem.Item.Name);
			}
		}
	}
}
