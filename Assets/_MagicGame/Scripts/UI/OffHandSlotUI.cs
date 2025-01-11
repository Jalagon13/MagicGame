using System;
using UnityEngine;

public class OffHandSlotUI : MonoBehaviour
{
	private void Start()
	{
		GameInput.Instance.OnSecondaryAction += GameInput_ExecuteOffHandItemAction;
	}

	private void GameInput_ExecuteOffHandItemAction(object sender, GameInput.OnPrimaryOrSecondaryActionEventArgs e)
	{
		if(transform.GetChild(0).TryGetComponent(out InventorySlotUI inventorySlotUI))
		{
			
		}
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnSecondaryAction -= GameInput_ExecuteOffHandItemAction;
	}
}
