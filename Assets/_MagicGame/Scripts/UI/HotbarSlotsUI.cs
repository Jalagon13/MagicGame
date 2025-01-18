using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarSlotsUI : MonoBehaviour 
{
	[SerializeField] private Color _highlightedColor;
	[SerializeField] private Color _unHighlightedColor;

	private void Start()
	{
		HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnFocusItemSet;
	}

	private void HotbarManager_OnFocusItemSet(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		for (int i = 0; i < transform.childCount; i++)
		{
			if(i == e.FocusItemSlotIndex)
			{
				HighlightSlot(transform.GetChild(i));
			}
			else
			{
				UnHighlighSlot(transform.GetChild(i));
			}
		}
	}

	private void HighlightSlot(Transform transform)
	{
		InventorySlotUI invSlotUI = transform.GetComponent<InventorySlotUI>();
		invSlotUI.ChangeColor(_highlightedColor);
		
		if(Player.LocalClientInstance != null)
		{
			SoundManager.Instance.PlayOneShot(FMODEvents.Instance.FocusSlotChanged, Player.LocalClientInstance.transform.position);
		}
	}
	
	private void UnHighlighSlot(Transform transform)
	{
		InventorySlotUI invSlotUI = transform.GetComponent<InventorySlotUI>();
		invSlotUI.ChangeColor(_unHighlightedColor);
	}


	private void OnDestroy()
	{
		HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnFocusItemSet;
	}
}
