using System.Collections.Generic;
using UnityEngine;

public class ChestObject : ResourceObject
{
	// Network variable for chest inventory for multipalyer

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
