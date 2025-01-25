using System.Collections.Generic;
using UnityEngine;

public class ChestObject : ResourceObject
{
	private void Awake()
	{
		ChestManager.Instance.TryToCreateEmptyChestData(Vector2Int.FloorToInt(transform.position));
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
}
