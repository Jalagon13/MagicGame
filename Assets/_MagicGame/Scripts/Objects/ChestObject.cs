using System;
using System.Collections.Generic;
using UnityEngine;

public class ChestObject : WorldObject
{
	[SerializeField] private WorldInput _worldInput;
	[SerializeField] private float _chestOpenDistance = 2.75f; 

	private void Start()
	{
		if(Player.LocalClientInstance.IsHost)
		{
			ChestManager.Instance.TryToCreateEmptyChestData(Vector2Int.FloorToInt(transform.position), Player.LocalClientInstance.CurrentBiome.Value);
		}
		
		GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
	}

	private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
	{
		var centerOfChestPosition = new Vector2(transform.position.x + 0.5f, transform.position.y + 0.5f);
		var playerInRange = Vector2.Distance(Player.LocalClientInstance.transform.position, centerOfChestPosition) <= _chestOpenDistance;
		
		if(_worldInput.IsMouseOverIndputDetector() && playerInRange)
		{
			ChestManager.Instance.OpenChest(Vector2Int.FloorToInt(transform.position), Player.LocalClientInstance.CurrentBiome.Value);
		}
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
	}
}
