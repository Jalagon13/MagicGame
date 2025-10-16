using System;
using System.Collections.Generic;
using UnityEngine;


namespace ProjectTinker
{
	public class ChestObject : ResourceObject
	{
		[field: SerializeField] public WorldInput WorldInput { get; private set; }

		private void Start()
		{
			if(Player.Instance.IsHost)
			{
				ChestManager.Instance.TryToCreateEmptyChestData(Vector2Int.FloorToInt(transform.position), Player.Instance.CurrentBiome.Value);
			}
		
			GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
		}

		private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
		{
			var centerOfChestPosition = new Vector2(transform.position.x + 0.5f, transform.position.y + 0.5f);
		
			if(WorldInput.IsMouseOverIndputDetector() && PlayerInRangeOfPosition(centerOfChestPosition))
			{
				ChestManager.Instance.RequestChestData(Vector2Int.FloorToInt(transform.position), Player.Instance.CurrentBiome.Value, gameObject);
			}
		}
	
		private void OnDestroy()
		{
			GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
		}
	}

}