using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class PortalObject : WorldObject
{
	[field: SerializeField] public BiomeType DestinationBiome { get; private set; }
	[field: SerializeField] public WorldInput WorldInput { get; private set; }
	
	private void Start()
	{
		GameInput.Instance.OnSecondaryAction += GameInput_OnSecondaryAction;
	}

	private void GameInput_OnSecondaryAction(object sender, GameInput.OnPrimaryOrSecondaryActionEventArgs e)
	{
		var centerOfChestPosition = new Vector2(transform.position.x + 0.5f, transform.position.y + 0.5f);

		if (WorldInput.IsMouseOverIndputDetector() && PlayerInRangeOfPosition(centerOfChestPosition))
		{
			WorldManager.Instance.LoadBiome(DestinationBiome, transform.position);
		}
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnSecondaryAction -= GameInput_OnSecondaryAction;
	}
}
