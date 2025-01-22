using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellBookInventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private Image _backgroundIcon;
	[SerializeField] private Image _spellIcon;

	public void Initialize(SpellProjectileItemSO spell)
	{
		
	}

    public void OnPointerClick(PointerEventData eventData)
    {
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
    }
}
