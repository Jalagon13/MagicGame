using System;
using System.Collections.Generic;
using UnityEngine;

public class ChestObject : WorldObject
{
	[field: SerializeField] public WorldInput WorldInput { get; private set; }

	private void Start()
	{
		if(Player.LocalClientInstance.IsHost)
		{
			ChestManager.Instance.TryToCreateEmptyChestData(Vector2Int.FloorToInt(transform.position), Player.LocalClientInstance.CurrentPlayerBiome.Value);
		}
		
		GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
	}

	private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
	{
		var centerOfChestPosition = new Vector2(transform.position.x + 0.5f, transform.position.y + 0.5f);
		
		if(WorldInput.IsMouseOverIndputDetector() && PlayerInRangeOfPosition(centerOfChestPosition))
		{
			ChestManager.Instance.RequestChestData(Vector2Int.FloorToInt(transform.position), Player.LocalClientInstance.CurrentPlayerBiome.Value, gameObject);
		}
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
	}
}
