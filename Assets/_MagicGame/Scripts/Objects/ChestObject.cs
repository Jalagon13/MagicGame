using System;
using System.Collections.Generic;
using UnityEngine;

public class ChestObject : WorldObject
{
	[SerializeField] private WorldInput _worldInput;
	[SerializeField] private float _chestOpenDistance = 2.75f; 

	private void Start()
	{
		ChestManager.Instance.TryToCreateEmptyChestData(Vector2Int.FloorToInt(transform.position));
		GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
	}

    private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
    {
		var centerOfChestPosition = new Vector2(transform.position.x + 0.5f, transform.position.y + 0.5f);
		var playerInRange = Vector2.Distance(Player.LocalClientInstance.transform.position, centerOfChestPosition) <= _chestOpenDistance;
		
		if(_worldInput.IsMouseOverIndputDetector() && playerInRange)
		{
			Debug.Log("world detector working and accessed");
			ChestManager.Instance.OpenChest(Vector2Int.FloorToInt(transform.position), Player.LocalClientInstance.PlayerEnvironment.Value);
		}
    }

	public List<ItemFileData> GetChestItems()
	{
		// get the items here and turn it into file data list
	
		return null;
	}
	
	public void DeserializeFileItemsToChest(List<ItemFileData> chestItems)
	{
		// override current inventory with these items
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
	}
}
