using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class PortalObject : WorldObject
{
	[SerializeField] private WorldInput _worldInput;
	
	private void Start()
	{
		GameInput.Instance.OnSecondaryAction += GameInput_OnSecondaryAction;
	}

	private void GameInput_OnSecondaryAction(object sender, GameInput.OnPrimaryOrSecondaryActionEventArgs e)
	{
		if(!_worldInput.IsMouseOverIndputDetector()) return;
		Debug.Log($"Portal Right clicked");
		// Just hard code it like this for now will change once more environments are added
		BiomeType destination = Player.LocalClientInstance.CurrentBiome.Value == BiomeType.Cave ? BiomeType.Forest : BiomeType.Cave;
		WorldManager.Instance.LoadBiome(destination, transform.position);
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnSecondaryAction -= GameInput_OnSecondaryAction;
	}
}
