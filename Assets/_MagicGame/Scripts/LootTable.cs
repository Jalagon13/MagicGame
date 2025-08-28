using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


[Serializable]
public class Loot 
{
	public ItemSO Item;
	public int Min = 1;
	public int Max = 1;
	[Range(0.0f, 100.0f)]
	public float Chance = 100f;
}

public static class LootTable
{
	public static void SpawnLoot(List<Loot> lootTable, Vector2 spawnPos, BiomeType biome, Vector2 velocity = default, float startingZAxis = 0f)
	{
		var itemsToSpawn = GetItemsToSpawn(lootTable);

		if(itemsToSpawn.Count > 0)
		{
			foreach (var itemsToSpawnKVP in itemsToSpawn)
			{
				// Spawn Loot here.
				ItemSO itemToSpawn = itemsToSpawnKVP.Key;
				int amountToSpawn = itemsToSpawnKVP.Value;	
				
				GameManager.Instance.SpawnItem(new InventoryItem(itemToSpawn, amountToSpawn), spawnPos, biome, velocity, startingZAxis);
			}
		}
	}
	
	private static Dictionary<ItemSO, int> GetItemsToSpawn(List<Loot> lootTable)
	{
		Dictionary<ItemSO, int> lootToDrop = new();

		foreach (Loot loot in lootTable)
		{
			if (Random.Range(0, 100) < loot.Chance)
			{
				int dropAmount = Random.Range(loot.Min, loot.Max + 1);

				if (lootToDrop.TryGetValue(loot.Item, out int existingAmount))
				{
					lootToDrop[loot.Item] = existingAmount + dropAmount;
				}
				else
				{
					lootToDrop.Add(loot.Item, dropAmount);
				}
			}
		}

		return lootToDrop;
	}
}

