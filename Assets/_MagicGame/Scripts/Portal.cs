using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class Portal : MonoBehaviour
{
	[SerializeField] private WorldInput _worldInput;
	
	private void Start()
	{
		GameInput.Instance.OnSecondaryAction += GameInput_OnSecondaryAction;
	}

	private void GameInput_OnSecondaryAction(object sender, GameInput.OnPrimaryOrSecondaryActionEventArgs e)
	{
		if(!_worldInput.IsMouseOverIndputDetector()) return;
		
		// Just hard code it like this for now will change once more environments are added
		EnvironmentID destination = Player.LocalClientInstance.PlayerEnvironment.Value == EnvironmentID.Cave ? EnvironmentID.Forest : EnvironmentID.Cave;
		WorldManager.Instance.LoadEnvironment(destination, transform.position);
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnSecondaryAction -= GameInput_OnSecondaryAction;
	}
}
