using System;
using AdvancedTooltips.Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuUI : MonoBehaviour
{
	private void Start()
	{
		GameInput.Instance.OnInventoryToggle += GameInput_OnInventoryToggle;
		
		Hide();
	}

	private void GameInput_OnInventoryToggle(object sender, GameInput.OnToggleInventoryEventArgs e)
	{
		if(e.InventoryOpen)
		{
			Show();
		}
		else
		{
			Hide();
		}
	}

	private void Show()
	{
		gameObject.SetActive(true);
		
		SoundManager.Instance.PlayOneShot(FMODEvents.Instance.InventoryOpen, Player.LocalClientInstance.transform.position);
	}
	
	private void Hide()
	{
		Tooltip.HideUI();
		gameObject.SetActive(false);
		
		if(Player.LocalClientInstance != null)
		{
			SoundManager.Instance.PlayOneShot(FMODEvents.Instance.InventoryClose, Player.LocalClientInstance.transform.position);
		}
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnInventoryToggle -= GameInput_OnInventoryToggle;
	}
}
