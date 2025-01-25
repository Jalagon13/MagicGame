using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChestManager : NetworkBehaviour
{
	public static ChestManager Instance { get; private set; }
	
	private Dictionary<Vector2Int, List<ChestItemData>> _forestChests = new();
	private class ChestItemData
	{
		public int SlotIndex;
		public int ItemId;
		public int ItemAmount;
	}
	
	private void Awake()
	{
		Instance = this;
	}
	
	public void TryToCreateEmptyChestData(Vector2Int chestPosition)
	{
		if(_forestChests.ContainsKey(chestPosition))
		{
			Debug.LogWarning($"A chest entry is already created for this position: {chestPosition}");
			return;
		}
		
		// Create an entry for this position with an empty chest
		_forestChests.Add(chestPosition, new());
		Debug.Log("New empty chest entry added for position " + chestPosition);
	}
	
	public void RemoveChestData()
	{
		
	}
}
