using System;
using UnityEngine;

public class DoorObject : ResourceObject
{
	[SerializeField] private WorldInput _worldInput;
	[SerializeField] private float _doorOpenDistance = 2.75f; 

	protected override void Start()
	{
		base.Start();
		
		GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
	}

	private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
	{
		Debug.Log($"Opening door");
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
	}
}
